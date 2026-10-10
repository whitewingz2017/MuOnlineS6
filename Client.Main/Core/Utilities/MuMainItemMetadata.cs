// Snapshot of MuMain src/bin/Data/Items and Items/Models. Keep client Item.bmd stats authoritative.
using Microsoft.Xna.Framework;
using Client.Main.Controls.UI.Game.Inventory;
namespace Client.Main.Core.Utilities;
internal static class MuMainItemMetadata
{
    public static int Key(ItemDefinition d) => d.Group * 512 + d.Id;
    public static bool IsSecondWing(ItemDefinition d) => Key(d) is 6147 or 6148 or 6149 or 6150 or 6186;
    public static bool IsThirdWing(ItemDefinition d) => Key(d) is 6180 or 6181 or 6182 or 6183 or 6184 or 6187 or 6194;
    public static bool IsWing(ItemDefinition d) => Key(d) is 6144 or 6145 or 6146 or 6147 or 6148 or 6149 or 6150 or 6180 or 6181 or 6182 or 6183 or 6184 or 6185 or 6186 or 6187 or 6193 or 6194 or 6274 or 6275 or 6276 or 6277 or 6278 or 6279 or 6686;
    public static bool IsSummonerBook(ItemDefinition d) => Key(d) is 2581 or 2582 or 2583;
    public static bool IsArchangel(ItemDefinition d) => Key(d) is 19 or 1037 or 2066 or 2570 or 2596;
    public static Vector3 InventoryRotation(ItemDefinition d) => Key(d) switch
    {
        0 or 1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 13 or 14 or 16 or 19 or 20 or 22 or 27 or 512 or 513 or 514 or 515 or 516 or 1024 or 1025 or 1026 or 1031 or 1032 or 1033 or 1034 or 1035 or 1036 or 1037 or 1040 or 1041 or 2560 or 2565 or 2566 or 2569 or 2570 or 2571 or 2574 or 2575 or 2576 or 2577 or 2578 or 2579 or 2580 or 2596 => new(180f, 270f, 15f),
        9 or 10 or 11 or 12 or 15 or 17 or 18 or 21 or 23 or 25 or 31 or 517 or 518 or 519 or 520 or 1027 or 1028 or 1029 or 1030 or 1537 or 1538 or 1539 or 1540 or 1541 or 1542 or 1543 or 1544 or 1545 or 2048 or 2049 or 2050 or 2051 or 2052 or 2053 or 2054 or 2561 or 2562 or 2563 or 2564 or 2568 => new(180f, 270f, 25f),
        24 or 1547 => new(180f, 90f, 15f),
        26 or 28 => new(180f, 270f, 10f),
        32 or 3614 or 3615 or 3619 or 4125 or 4126 or 4130 or 4131 or 4132 or 4133 or 4134 or 4135 or 4136 or 4137 or 4138 or 4139 or 4140 or 6193 or 6686 or 6737 or 6738 or 6739 or 6740 or 6741 or 6742 or 6743 or 6744 or 6745 or 6746 or 6747 or 6748 or 6749 or 6750 => new(-90f, 0f, 0f),
        33 or 34 or 35 or 2581 or 2582 or 2583 or 4155 or 4156 or 4157 or 6214 or 6215 or 6216 or 6244 or 6245 or 6246 or 6247 or 6248 or 6249 or 6250 or 6251 or 6252 or 6253 or 6254 or 6255 or 6256 or 6257 or 6258 or 6259 or 6260 or 6261 or 6262 or 6263 or 6264 or 6265 or 6266 or 6267 or 6268 or 6269 or 6270 or 6271 or 6272 or 6273 or 6759 or 7240 or 7241 or 7242 or 7243 or 7244 or 7245 or 7265 or 7266 or 7268 or 7277 or 7308 => new(0f, 0f, 0f),
        1038 or 1039 => new(180f, 90f, 13f),
        1042 => new(180f, 90f, 2f),
        1536 => new(0f, 90f, 20f),
        1546 => new(180f, 270f, 20f),
        2055 or 2063 => new(0f, 270f, 15f),
        2056 or 2057 or 2058 or 2059 or 2060 or 2061 or 2062 or 2064 or 2066 or 2067 => new(90f, 180f, 20f),
        2065 => new(0f, 90f, 15f),
        2068 or 2069 or 2070 or 2071 or 2072 => new(180f, -90f, 15f),
        2567 => new(0f, 0f, 205f),
        2572 => new(180f, 0f, 8f),
        2573 => new(180f, 90f, 8f),
        2590 or 2591 or 2592 or 2593 or 2594 => new(180f, 90f, 10f),
        3072 or 3073 or 3074 or 3075 or 3076 or 3077 or 3078 or 3079 or 3080 or 3081 or 3082 or 3083 or 3084 or 3085 or 3086 or 3087 or 3088 or 3089 or 3090 or 3091 or 3092 or 3093 => new(270f, 270f, 0f),
        3584 or 3585 or 3586 or 3587 or 3588 or 3589 or 3590 or 3591 or 3592 or 3593 or 3594 or 3595 or 3596 or 3597 or 3598 or 3600 or 3601 or 3602 or 3603 or 3605 or 3606 or 3608 or 3609 or 3610 or 3611 or 3612 or 3613 or 3617 or 3618 or 3620 or 3622 or 3629 or 3630 or 3633 or 3634 or 3635 or 3636 or 3637 or 3644 or 3645 or 3646 or 3647 or 3648 or 3649 or 3650 or 3651 or 3652 or 3653 or 3654 or 3656 or 3657 or 4096 or 4097 or 4098 or 4099 or 4100 or 4101 or 4102 or 4103 or 4104 or 4105 or 4108 or 4109 or 4110 or 4111 or 4112 or 4113 or 4114 or 4115 or 4116 or 4117 or 4118 or 4119 or 4120 or 4121 or 4122 or 4123 or 4124 or 4127 or 4128 or 4129 or 4141 or 4142 or 4143 or 4144 or 4145 or 4146 or 4147 or 4148 or 4149 or 4158 or 4159 or 4160 or 4161 or 4162 or 4163 or 4164 or 4165 or 4166 or 4167 or 4168 or 4608 or 4609 or 4610 or 4611 or 4612 or 4613 or 4614 or 4615 or 4616 or 4617 or 4620 or 4621 or 4622 or 4623 or 4624 or 4625 or 4626 or 4627 or 4628 or 4629 or 4630 or 4631 or 4632 or 4633 or 4634 or 4635 or 4636 or 4637 or 4638 or 4639 or 4640 or 4641 or 4642 or 4643 or 4644 or 4645 or 4646 or 4647 or 4648 or 4649 or 4650 or 4651 or 4652 or 4653 or 4654 or 4655 or 4656 or 4657 or 4658 or 4659 or 4660 or 4661 or 4667 or 4668 or 4669 or 4670 or 4671 or 4672 or 4673 or 4674 or 4675 or 4676 or 4677 or 4678 or 4679 or 4680 or 4681 or 5120 or 5121 or 5122 or 5123 or 5124 or 5125 or 5126 or 5127 or 5128 or 5129 or 5130 or 5131 or 5132 or 5133 or 5134 or 5135 or 5136 or 5137 or 5138 or 5139 or 5140 or 5141 or 5142 or 5143 or 5144 or 5145 or 5146 or 5147 or 5148 or 5149 or 5150 or 5151 or 5152 or 5153 or 5154 or 5155 or 5156 or 5157 or 5158 or 5159 or 5160 or 5161 or 5162 or 5163 or 5164 or 5165 or 5166 or 5167 or 5168 or 5169 or 5170 or 5171 or 5172 or 5173 or 5182 or 5183 or 5184 or 5185 or 5186 or 5187 or 5188 or 5189 or 5190 or 5191 or 5632 or 5633 or 5634 or 5635 or 5636 or 5637 or 5638 or 5639 or 5640 or 5641 or 5642 or 5643 or 5644 or 5645 or 5646 or 5647 or 5648 or 5649 or 5650 or 5651 or 5652 or 5653 or 5654 or 5655 or 5656 or 5657 or 5658 or 5659 or 5660 or 5661 or 5662 or 5663 or 5664 or 5665 or 5666 or 5667 or 5668 or 5669 or 5670 or 5671 or 5672 or 5673 or 5674 or 5675 or 5676 or 5677 or 5678 or 5679 or 5680 or 5681 or 5682 or 5683 or 5684 or 5685 or 5691 or 5692 or 5693 or 5694 or 5695 or 5696 or 5697 or 5698 or 5699 or 5700 or 5701 or 5702 or 5703 or 5704 or 5705 or 6144 or 6145 or 6146 or 6147 or 6148 or 6149 or 6150 or 6152 or 6153 or 6154 or 6155 or 6156 or 6157 or 6158 or 6159 or 6160 or 6161 or 6162 or 6163 or 6164 or 6165 or 6166 or 6167 or 6168 or 6174 or 6175 or 6176 or 6177 or 6178 or 6179 or 6180 or 6181 or 6182 or 6183 or 6184 or 6185 or 6187 or 6192 or 6194 or 6217 or 6218 or 6274 or 6275 or 6276 or 6277 or 6278 or 6279 or 6280 or 6281 or 6282 or 6284 or 6285 or 6656 or 6657 or 6658 or 6663 or 6664 or 6665 or 6666 or 6667 or 6670 or 6671 or 6672 or 6673 or 6720 or 6721 or 7168 or 7169 or 7170 or 7171 or 7172 or 7173 or 7174 or 7175 or 7176 or 7177 or 7178 or 7179 or 7181 or 7182 or 7183 or 7184 or 7185 or 7186 or 7187 or 7189 or 7190 or 7191 or 7192 or 7193 or 7194 or 7196 or 7197 or 7199 or 7203 or 7204 or 7205 or 7206 or 7207 or 7208 or 7210 or 7213 or 7214 or 7215 or 7216 or 7217 or 7218 or 7219 or 7236 or 7269 or 7278 or 7279 or 7289 or 7290 or 7291 or 7292 or 7309 or 7310 or 7311 or 7312 or 7680 or 7681 or 7682 or 7683 or 7684 or 7685 or 7686 or 7687 or 7688 or 7689 or 7690 or 7691 or 7692 or 7693 or 7694 or 7695 or 7696 or 7697 or 7698 or 7708 or 7709 => new(270f, -10f, 0f),
        3623 or 3624 or 3625 or 3626 or 3627 or 3628 or 3643 => new(-90f, 25f, 0f),
        4106 or 4107 or 4618 or 4619 or 6151 or 6188 or 6189 or 6190 or 6191 or 6688 or 6689 or 6690 or 6691 or 6692 or 6693 or 6695 or 6696 or 6697 or 6698 or 6699 or 6700 or 6701 or 6702 or 6703 or 6704 or 6705 or 6706 or 6707 or 6708 or 6710 or 6711 or 6712 or 6713 or 6714 or 6716 or 6717 or 6718 or 6722 or 6727 or 6728 or 6729 or 6730 or 6731 or 6732 or 6753 or 6754 or 6760 or 6761 or 6769 or 6770 or 6771 or 6772 or 6777 or 6780 or 6781 or 6782 or 6783 or 6791 or 6792 or 6793 or 6794 or 6795 or 6796 or 6797 or 6798 or 6799 or 6800 or 7188 or 7195 or 7222 or 7226 or 7227 or 7228 or 7229 or 7230 or 7232 or 7233 or 7234 or 7235 or 7238 or 7239 or 7246 or 7247 or 7248 or 7249 or 7250 or 7251 or 7252 or 7253 or 7254 or 7255 or 7256 or 7257 or 7258 or 7259 or 7260 or 7261 or 7262 or 7263 or 7264 or 7282 or 7283 or 7284 or 7285 or 7286 or 7287 or 7288 or 7294 or 7295 or 7296 or 7297 or 7298 or 7299 or 7300 or 7301 or 7302 or 7303 or 7304 or 7305 or 7306 or 7307 or 7313 or 7314 or 7315 or 7316 or 7317 or 7318 or 7328 or 7329 or 7699 or 7700 or 7701 or 7702 or 7703 or 7704 or 7705 or 7706 or 7707 or 7710 or 7711 or 7712 or 7713 or 7714 or 7715 or 7716 => new(270f, 0f, 0f),
        4169 or 6715 or 6719 => new(90f, 0f, 0f),
        6186 => new(270f, 0f, 2f),
        6204 or 6205 or 6206 or 6207 or 6208 or 6209 => new(10f, -10f, 10f),
        6283 or 7180 or 7209 => new(270f, 90f, 0f),
        6286 or 6287 or 7211 or 7212 => new(270f, -10f, -45f),
        6659 or 6660 or 6687 => new(-90f, -90f, 0f),
        6661 => new(-90f, -35f, 0f),
        6668 or 6669 or 6681 or 6682 or 6683 or 6684 => new(360f, 0f, 0f),
        6674 or 6685 => new(290f, 0f, 0f),
        6677 or 6678 or 6679 or 6680 => new(270f, 160f, 20f),
        6694 => new(-198f, 0f, 0f),
        6709 => new(270f, 120f, 0f),
        6723 or 6736 or 6779 => new(270f, 40f, 0f),
        6724 => new(300f, 10f, 20f),
        6725 => new(270f, -30f, 0f),
        6726 => new(270f, 0f, 70f),
        6762 => new(255f, 45f, 0f),
        6763 => new(90f, 225f, 45f),
        6765 or 6766 or 6767 or 6768 => new(270f, 25f, 25f),
        6778 => new(290f, -20f, 0f),
        7200 or 7201 or 7202 => new(0f, -10f, 0f),
        7220 => new(270f, -25f, 0f),
        7221 => new(180f, 0f, 0f),
        7231 => new(-50f, -60f, 0f),
        7267 => new(290f, -40f, 0f),
        7270 or 7271 or 7272 or 7273 or 7274 or 7275 or 7276 => new(0f, 0f, 30f),
        7280 or 7281 => new(270f, 180f, 45f),
        _ => new(270f, -10f, 0f),
    };
}
