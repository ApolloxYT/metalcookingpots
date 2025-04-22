using MetalPots.Blocks;
using MetalPots.System.Cooking;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace MetalPots
{
    public class MetalPotsModSystem : ModSystem
    {

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass(Mod.Info.ModID + ".MPBlockCookingContainer", typeof(MPBlockCookingContainers));
            api.RegisterBlockClass(Mod.Info.ModID + ".MPBlockCookedContainer", typeof(MPBlockCookedContainer));
            Mod.Logger.Notification("Hello from template mod: " + api.Side);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("metalpots:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("metalpots:hello"));
        }

    }
}
