﻿/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Core;
using OpenNos.Core.Networking.Communication.Scs.Communication;
using System;
using System.Threading;

namespace OpenNos.GameObject
{
    public class FoodItem : Item
    {
        #region Methods

        public void Regenerate(ClientSession session, Item item)
        {
            session.Character.IsSitting = true;
            session.CurrentMap?.Broadcast(session, session.Character.GenerateRest(), ReceiverType.All);

            session.Client.SendPacket(session.Character.GenerateEff(6000));
            session.Character.SnackAmount++;
            session.Character.MaxSnack = 0;
            session.Character.SnackHp += item.Hp / 5;
            session.Character.SnackMp += item.Mp / 5;
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(1800);
            }
            session.Character.SnackHp = item.Hp / 5;
            session.Character.SnackMp = item.Mp / 5;
            session.Character.SnackAmount--;
        }

        public void Sync(ClientSession session, Item item)
        {
            for (session.Character.MaxSnack = 0; session.Character.MaxSnack < 5 && session.Character.IsSitting; session.Character.MaxSnack++)
            {
                session.Character.Mp += session.Character.SnackHp;
                session.Character.Hp += session.Character.SnackMp;
                if ((session.Character.SnackHp > 0 && session.Character.SnackHp > 0) && (session.Character.Hp < session.Character.HPLoad() || session.Character.Mp < session.Character.MPLoad()))
                    session.CurrentMap?.Broadcast(session, session.Character.GenerateRc(session.Character.SnackHp), ReceiverType.All);
                if (session.Client.CommunicationState == CommunicationStates.Connected)
                    session.Client.SendPacket(session.Character.GenerateStat());
                else return;
                Thread.Sleep(1800);
            }
        }

        public override void Use(ClientSession session, ref Inventory Inv)
        {
            if ((DateTime.Now - session.Character.LastPotion).TotalMilliseconds < 750)
                return;
            else
                session.Character.LastPotion = DateTime.Now;
            Item item = ServerManager.GetItem(Inv.ItemInstance.ItemVNum);
            switch (Effect)
            {
                default:
                    if (session.Character.IsSitting == false)
                    {
                        session.Character.SnackAmount = 0;
                        session.Character.SnackHp = 0;
                        session.Character.SnackMp = 0;
                    }
                    int amount = session.Character.SnackAmount;
                    if (amount < 5)
                    {
                        Thread workerThread = new Thread(() => Regenerate(session, item));
                        workerThread.Start();
                        Inv.ItemInstance.Amount--;
                        if (Inv.ItemInstance.Amount > 0)
                            session.Client.SendPacket(session.Character.GenerateInventoryAdd(Inv.ItemInstance.ItemVNum, Inv.ItemInstance.Amount, Inv.Type, Inv.Slot, 0, 0, 0, 0));
                        else
                        {
                            session.Character.InventoryList.DeleteFromSlotAndType(Inv.Slot, Inv.Type);
                            session.Client.SendPacket(session.Character.GenerateInventoryAdd(1, 0, Inv.Type, Inv.Slot, 0, 0, 0, 0));
                        }
                    }
                    else
                    {
                        if (session.Character.Gender == 1)
                            session.Client.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_HUNGRY_FEMALE"), 1));
                        else
                            session.Client.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_HUNGRY_MALE"), 1));
                    }
                    if (amount == 0)
                    {
                        Thread workerThread2 = new Thread(() => Sync(session, item));
                        workerThread2.Start();
                    }
                    break;
            }
        }

        #endregion
    }
}