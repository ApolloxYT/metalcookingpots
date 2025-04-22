using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using MetalPots.BlockEntityRenderer;
using MetalPots.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace MetalPots.System.Cooking
{
    internal class MPBlockCookingContainers: BlockCookingContainer, IInFirepitRendererSupplier
    {
        new public IInFirepitRenderer GetRendererWhenInFirepit(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
        {
            return new MetalPotInFirepitRenderer(api as ICoreClientAPI, stack, firepit.Pos, forOutputSlot);
        }
        public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
        {
            ItemStack[] stacks = GetCookingStacks(cookingSlotsProvider);
            CookingRecipe recipe = GetMatchingCookingRecipe(world, stacks);

            Block block = world.GetBlock(CodeWithVariant("type", "cooked"));

            if (recipe == null) return;

            int quantityServings = recipe.GetQuantityServings(stacks);

            if (recipe.CooksInto != null)
            {
                var outstack = recipe.CooksInto.ResolvedItemstack?.Clone();
                if (outstack != null)
                {
                    outstack.StackSize *= quantityServings;
                    stacks = new ItemStack[] { outstack };
                    if (!outstack.Attributes.HasAttribute("notDirtied")) block = world.GetBlock(new AssetLocation(Attributes["dirtiedBlockCode"].AsString()));
                    if (outstack.Attributes.HasAttribute("notDirtied"))
                    {
                        block = world.GetBlock(new AssetLocation(world.GetBlock(new AssetLocation(Attributes["mealBlockCode"].AsString())).Attributes["emptiedBlockCode"].AsString()));
                        outstack.Attributes.RemoveAttribute("notDirtied");
                    }
                }
            }
            else
            {
                for (int i = 0; i < stacks.Length; i++)
                {
                    CookingRecipeIngredient ingred = recipe.GetIngrendientFor(stacks[i]);
                    ItemStack cookedStack = ingred.GetMatchingStack(stacks[i])?.CookedStack?.ResolvedItemstack.Clone();
                    if (cookedStack != null)
                    {
                        stacks[i] = cookedStack;
                    }
                }
            }

            ItemStack outputStack = new ItemStack(block);
            outputStack.Collectible.SetTemperature(world, outputStack, GetIngredientsTemperature(world, stacks));

            // Carry over and set perishable properties
            TransitionableProperties cookedPerishProps = recipe.PerishableProps.Clone();
            cookedPerishProps.TransitionedStack.Resolve(world, "cooking container perished stack");

            CarryOverFreshness(api, cookingSlotsProvider.Slots, stacks, cookedPerishProps);


            if (recipe.CooksInto != null)
            {
                for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
                {
                    cookingSlotsProvider.Slots[i].Itemstack = i == 0 ? stacks[0] : null;
                }
                inputSlot.Itemstack = outputStack;
            }
            else
            {
                for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
                {
                    cookingSlotsProvider.Slots[i].Itemstack = null;
                }
                ((MPBlockCookedContainer)block).SetContents(recipe.Code, quantityServings, outputStack, stacks);

                outputSlot.Itemstack = outputStack;
                inputSlot.Itemstack = null;
            }
        }
    }
}
