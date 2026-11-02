/////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Audiokinetic Wwise generated include file. Do not edit.
//
/////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef __WWISE_IDS_H__
#define __WWISE_IDS_H__

#include <AK/SoundEngine/Common/AkTypes.h>

namespace AK
{
    namespace EVENTS
    {
        static const AkUniqueID PLAY_BGM = 3126765036U;
        static const AkUniqueID PLAY_BOILING = 3626504988U;
        static const AkUniqueID PLAY_CUTTING = 1873669494U;
        static const AkUniqueID PLAY_DICE_ROLL = 3475817107U;
        static const AkUniqueID PLAY_EAT = 2489068396U;
        static const AkUniqueID PLAY_FOOTSTEP = 1602358412U;
        static const AkUniqueID PLAY_FRYING = 775664385U;
        static const AkUniqueID PLAY_OIL = 2959827418U;
        static const AkUniqueID PLAY_PEPPER = 2775506426U;
        static const AkUniqueID PLAY_SALT = 286864310U;
        static const AkUniqueID PLAY_SAUCE = 3281736991U;
        static const AkUniqueID PLAY_SPICE = 1614754728U;
        static const AkUniqueID PLAY_SUGAR = 1000563996U;
        static const AkUniqueID PLAY_UI_CANCEL = 2244506081U;
        static const AkUniqueID PLAY_UI_CLICK = 1749424733U;
        static const AkUniqueID PLAY_UI_PAGETURN = 1290567879U;
        static const AkUniqueID PLQY_AMBIENCE = 4100364718U;
        static const AkUniqueID STOP_AMBIENCE = 2477713992U;
        static const AkUniqueID STOP_BGM = 1073466678U;
        static const AkUniqueID STOP_BOILING = 2016713342U;
        static const AkUniqueID STOP_FRYING = 2718239607U;
    } // namespace EVENTS

    namespace STATES
    {
        namespace MUSIC_BGM_STAGE
        {
            static const AkUniqueID GROUP = 478818154U;

            namespace STATE
            {
                static const AkUniqueID BGM_V1 = 2962601787U;
                static const AkUniqueID BGM_V2 = 2962601784U;
                static const AkUniqueID BGM_V3 = 2962601785U;
                static const AkUniqueID NONE = 748895195U;
            } // namespace STATE
        } // namespace MUSIC_BGM_STAGE

        namespace MUSIC_GAMEPHASE
        {
            static const AkUniqueID GROUP = 524889184U;

            namespace STATE
            {
                static const AkUniqueID DAY = 311764537U;
                static const AkUniqueID MENU = 2607556080U;
                static const AkUniqueID NIGHT = 1011622525U;
                static const AkUniqueID NONE = 748895195U;
            } // namespace STATE
        } // namespace MUSIC_GAMEPHASE

    } // namespace STATES

    namespace SWITCHES
    {
        namespace DISH
        {
            static const AkUniqueID GROUP = 2212414297U;

            namespace SWITCH
            {
                static const AkUniqueID FOOD_FRIEDFISH = 1887427020U;
                static const AkUniqueID FOOD_HOTPOT = 1053467246U;
                static const AkUniqueID FOOD_JELLYSHIP = 3530175662U;
                static const AkUniqueID FOOD_RAINBOWEGG = 3493444713U;
                static const AkUniqueID FOOD_SEAFOODRICE = 2870899870U;
                static const AkUniqueID FOOD_SOUP = 2824318541U;
                static const AkUniqueID FOOD_YAMPIE = 975081307U;
            } // namespace SWITCH
        } // namespace DISH

        namespace INGREDIENT
        {
            static const AkUniqueID GROUP = 1746288848U;

            namespace SWITCH
            {
                static const AkUniqueID BEEF = 1123068863U;
                static const AkUniqueID BERRY = 302590163U;
                static const AkUniqueID CORNFISH = 1977508817U;
                static const AkUniqueID CUCUMBER = 1837441353U;
                static const AkUniqueID DONEEGG = 774000112U;
                static const AkUniqueID MUSHROOM = 1941802987U;
                static const AkUniqueID ONION = 2545240700U;
                static const AkUniqueID PUMPKIN = 2863832591U;
                static const AkUniqueID SHRIMP = 356833188U;
                static const AkUniqueID TOMATO = 3220806263U;
                static const AkUniqueID YAM = 912360420U;
            } // namespace SWITCH
        } // namespace INGREDIENT

    } // namespace SWITCHES

    namespace GAME_PARAMETERS
    {
        static const AkUniqueID SS_AIR_FEAR = 1351367891U;
        static const AkUniqueID SS_AIR_FREEFALL = 3002758120U;
        static const AkUniqueID SS_AIR_FURY = 1029930033U;
        static const AkUniqueID SS_AIR_MONTH = 2648548617U;
        static const AkUniqueID SS_AIR_PRESENCE = 3847924954U;
        static const AkUniqueID SS_AIR_RPM = 822163944U;
        static const AkUniqueID SS_AIR_SIZE = 3074696722U;
        static const AkUniqueID SS_AIR_STORM = 3715662592U;
        static const AkUniqueID SS_AIR_TIMEOFDAY = 3203397129U;
        static const AkUniqueID SS_AIR_TURBULENCE = 4160247818U;
    } // namespace GAME_PARAMETERS

    namespace BANKS
    {
        static const AkUniqueID INIT = 1355168291U;
        static const AkUniqueID MAINAUDIO = 1964102280U;
    } // namespace BANKS

    namespace BUSSES
    {
        static const AkUniqueID MAIN_AUDIO_BUS = 2246998526U;
        static const AkUniqueID MASTER_AUDIO_BUS = 3803692087U;
        static const AkUniqueID MUSIC = 3991942870U;
        static const AkUniqueID SFX = 393239870U;
        static const AkUniqueID UI = 1551306167U;
    } // namespace BUSSES

    namespace AUDIO_DEVICES
    {
        static const AkUniqueID NO_OUTPUT = 2317455096U;
        static const AkUniqueID SYSTEM = 3859886410U;
    } // namespace AUDIO_DEVICES

}// namespace AK

#endif // __WWISE_IDS_H__
