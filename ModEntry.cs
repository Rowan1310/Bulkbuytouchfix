using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace BulkBuyTouchFix
{
    public class ModEntry : Mod
    {
        public static IMonitor MyMonitor;
        public static IModHelper MyHelper;

        public override void Entry(IModHelper helper)
        {
            MyMonitor = Monitor;
            MyHelper = helper;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(ShopMenu), "setUpStoreForContext"),
                postfix: new HarmonyMethod(typeof(BulkPatch), nameof(BulkPatch.EnableBulk))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(ShopMenu), "receiveLeftClick"),
                prefix: new HarmonyMethod(typeof(BulkPatch), nameof(BulkPatch.OnClickPrefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(ShopMenu), "draw"),
                postfix: new HarmonyMethod(typeof(BulkPatch), nameof(BulkPatch.DrawButtons))
            );

            Monitor.Log("BulkBuy Touch Fix AKTIF - Semua toko bisa bulk!", LogLevel.Info);
        }
    }

    public static class BulkPatch
    {
        // Tombol kita
        public static Rectangle ButtonPlus1;
        public static Rectangle ButtonPlus10;
        public static Rectangle ButtonMax;
        public static int CurrentBulkAmount = 1;

        public static void EnableBulk(ShopMenu __instance)
        {
            // INI KUNCINYA: Paksa semua toko, termasuk Krobus, Sandy, Dwarf, Traveling Cart, Island Trader
            // bisa beli banyak
            __instance.CanPurchaseInBulk = true;
            CurrentBulkAmount = 1;
        }

        public static bool OnClickPrefix(ShopMenu __instance, int x, int y)
        {
            // Cek kalo yang di-tap itu tombol kita
            if (__instance.CanPurchaseInBulk && __instance.heldItem != null)
            {
                // Kita ambil posisi tombol dari Draw
                if (ButtonPlus1.Contains(x, y))
                {
                    CurrentBulkAmount = Math.Min(CurrentBulkAmount + 1, 999);
                    Game1.playSound("drumkit6");
                    return false; // Jangan lanjut ke klik bawaan game
                }
                if (ButtonPlus10.Contains(x, y))
                {
                    CurrentBulkAmount = Math.Min(CurrentBulkAmount + 10, 999);
                    Game1.playSound("drumkit6");
                    return false;
                }
                if (ButtonMax.Contains(x, y))
                {
                    // Hitung max yang bisa dibeli dari uang + stok
                    int maxByMoney = Game1.player.Money / Math.Max(1, __instance.heldItem.salePrice());
                    int maxByStock = __instance.heldItem.Stack == int.MaxValue ? 999 : __instance.heldItem.Stack;
                    CurrentBulkAmount = Math.Min(Math.Min(maxByMoney, maxByStock), 999);
                    if (CurrentBulkAmount < 1) CurrentBulkAmount = 1;
                    Game1.playSound("coin");
                    return false;
                }
            }
            return true; // Lanjut ke klik bawaan
        }

        public static void DrawButtons(ShopMenu __instance, SpriteBatch b)
        {
            if (!__instance.CanPurchaseInBulk || __instance.heldItem == null) return;

            // Posisi tombol di bawah item yang dipegang
            // Disesuaikan buat layar HP portrait/landscape
            int x = __instance.xPositionOnScreen + __instance.width - 380;
            int y = __instance.yPositionOnScreen + __instance.height - 110;

            ButtonPlus1 = new Rectangle(x, y, 100, 70);
            ButtonPlus10 = new Rectangle(x + 110, y, 100, 70);
            ButtonMax = new Rectangle(x + 220, y, 120, 70);

            // Gambar tombol - pake kotak coklat ala Stardew
            DrawButton(b, ButtonPlus1, "+1");
            DrawButton(b, ButtonPlus10, "+10");
            DrawButton(b, ButtonMax, "MAX");

            // Tampilkan jumlah yang mau dibeli
            string text = $"Beli: {CurrentBulkAmount}x";
            Vector2 textSize = Game1.dialogueFont.MeasureString(text);
            b.DrawString(Game1.dialogueFont, text, new Vector2(x + 50, y - 40), Color.White);
        }

        private static void DrawButton(SpriteBatch b, Rectangle rect, string label)
        {
            // Background
            b.Draw(Game1.staminaRect, rect, Color.SaddleBrown);
            // Border putih
            b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, rect.Width, 4), Color.White);
            b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y + rect.Height - 4, rect.Width, 4), Color.White);
            b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, 4, rect.Height), Color.White);
            b.Draw(Game1.staminaRect, new Rectangle(rect.X + rect.Width - 4, rect.Y, 4, rect.Height), Color.White);

            Vector2 size = Game1.dialogueFont.MeasureString(label);
            b.DrawString(Game1.dialogueFont, label, new Vector2(rect.X + (rect.Width - size.X) / 2, rect.Y + (rect.Height - size.Y) / 2), Color.White);
        }
    }
}
