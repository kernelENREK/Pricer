﻿using Pricer.Utility;
using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Pricer {
    /// <summary>
    /// PriceManager handles downlading, managing and translating price data from various websites
    /// </summary>
    public class PriceManager {
        private Dictionary<string, Entry> prices = new Dictionary<string, Entry>();

        /// <summary>
        /// Picks download source depending on source selection
        /// </summary>
        public void Download() {
            switch (Settings.source.ToLower()) {
                case "poe.ninja":
                    DownloadPoeNinjaData();
                    break;
                case "poe.ovh":
                    DownloadPoeOvhData();
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Downloads and populates price data from http://poe.ovh
        /// </summary>
        private void DownloadPoeOvhData() {
            // Clear previous data
            prices.Clear();

            try {
                // Download JSON-encoded string
                string jsonString = MainWindow.webClient.DownloadString("http://api.poe.ovh/compactPriceAPI?league=" + Settings.league);

                // Deserialize
                List<PoeOvhEntry> tempDict = new JavaScriptSerializer().Deserialize<List<PoeOvhEntry>>(jsonString);

                if (tempDict == null) return;

                // Add all values from temp dict to new dict (for ease of use)
                foreach (PoeOvhEntry ovhEntry in tempDict) {
                    // Create Entry instance
                    Entry entry = new Entry();

                    // Set Entry value
                    switch (Settings.method.ToLower()) {
                        case "mean":
                            entry.value = ovhEntry.mean;
                            break;
                        case "median":
                            entry.value = ovhEntry.median;
                            break;
                        case "mode":
                            entry.value = ovhEntry.mode;
                            break;
                        default:
                            break;
                    }

                    // Set misc data
                    entry.count = ovhEntry.count;

                    // Key came with league and category
                    String key = ovhEntry.key.Substring(ovhEntry.key.IndexOf('|', ovhEntry.key.IndexOf('|') + 1) + 1);

                    // Add to database
                    if (prices.ContainsKey(key)) {
                        Console.WriteLine("duplicate key: " + key);
                    } else {
                        prices.Add(key, entry);
                    }
                }

            } catch (Exception ex) {
                MainWindow.Log(ex.ToString(), 2);
            }
        }

        /// <summary>
        /// Downloads and populates price data from http://poe.ninja
        /// </summary>
        private void DownloadPoeNinjaData() {
            // Clear previous data
            prices.Clear();

            foreach (string key in Settings.poeNinjaKeys) {
                try {
                    // Download JSON-encoded string
                    string jsonString = MainWindow.webClient.DownloadString("http://poe.ninja/api/Data/Get" + 
                        key + "Overview?league=" + Settings.league);

                    // Deserialize JSON string
                    Dictionary<string, List<PoeNinjaEntry>> tempDict = new JavaScriptSerializer()
                        .Deserialize<Dictionary<string, List<PoeNinjaEntry>>>(jsonString);

                    if (tempDict == null) throw new Exception("Received no JSON for: " + key);

                    List<PoeNinjaEntry> entryList;
                    tempDict.TryGetValue("lines", out entryList);

                    if (entryList == null) throw new Exception("Got invalid JSON format for:" + key);

                    foreach (PoeNinjaEntry ninjaEntry in entryList) {
                        // Quick and dirty workarounds
                        Entry entry = new Entry { count = ninjaEntry.count };
                        string itemKey;

                        switch(key) {
                            case "Currency":
                                entry.value = ninjaEntry.chaosEquivalent;
                                itemKey = ninjaEntry.currencyTypeName + "|5";
                                if (!prices.ContainsKey(itemKey)) prices.Add(itemKey, entry);
                                break;

                            case "Fragment":
                                entry.value = ninjaEntry.chaosEquivalent;
                                itemKey = ninjaEntry.currencyTypeName + "|0";
                                if (!prices.ContainsKey(itemKey)) prices.Add(itemKey, entry);
                                break;

                            case "UniqueArmour":
                            case "UniqueWeapon":
                                entry.value = ninjaEntry.chaosValue;

                                switch (ninjaEntry.links) {
                                    case 6:
                                        itemKey = ninjaEntry.name + ":" + ninjaEntry.baseType + "|" + ninjaEntry.itemClass + "|links:6";
                                        break;
                                    case 5:
                                        itemKey = ninjaEntry.name + ":" + ninjaEntry.baseType + "|" + ninjaEntry.itemClass + "|links:5";
                                        break;
                                    default:
                                        itemKey = ninjaEntry.name + ":" + ninjaEntry.baseType + "|" + ninjaEntry.itemClass;
                                        break;
                                }

                                switch (ninjaEntry.name) {
                                    case "Atziri's Splendour":
                                        switch(ninjaEntry.variant) {
                                            case "Armour/ES":
                                                itemKey += "|var:ar/es";
                                                break;
                                            case "ES":
                                                itemKey += "|var:es";
                                                break;
                                            case "Armour/Evasion":
                                                itemKey += "|var:ar/ev";
                                                break;
                                            case "Armour/ES/Life":
                                                itemKey += "|var:ar/es/li";
                                                break;
                                            case "Evasion/ES":
                                                itemKey += "|var:ev/es";
                                                break;
                                            case "Armour/Evasion/ES":
                                                itemKey += "|var:ar/ev/es";
                                                break;
                                            case "Evasion":
                                                itemKey += "|var:ev";
                                                break;
                                            case "Evasion/ES/Life":
                                                itemKey += "|var:ev/es/li";
                                                break;
                                            case "Armour":
                                                itemKey += "|var:ar";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    case "Yriel's Fostering":
                                        switch (ninjaEntry.variant) {
                                            case "Bleeding":
                                                itemKey += "|var:ursa";
                                                break;
                                            case "Poison":
                                                itemKey += "|var:snake";
                                                break;
                                            case "Maim":
                                                itemKey += "|var:rhoa";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    case "Volkuur's Guidance":
                                        switch (ninjaEntry.variant) {
                                            case "Lightning":
                                                itemKey += "|var:lightning";
                                                break;
                                            case "Fire":
                                                itemKey += "|var:fire";
                                                break;
                                            case "Cold":
                                                itemKey += "|var:cold";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    case "Lightpoacher":
                                    case "Shroud of the Lightless":
                                    case "Bubonic Trail":
                                    case "Tombfist":
                                        switch (ninjaEntry.variant) {
                                            case "2 Jewels":
                                                itemKey += "|var:2 sockets";
                                                break;
                                            case "1 Jewel":
                                                itemKey += "|var:1 socket";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    default:
                                        break;
                                }
                                
                                if (!prices.ContainsKey(itemKey)) prices.Add(itemKey, entry);
                                break;

                            case "UniqueMap":
                            case "UniqueJewel":
                            case "UniqueFlask":
                            case "UniqueAccessory":
                                entry.value = ninjaEntry.chaosValue;
                                itemKey = ninjaEntry.name + ":" + ninjaEntry.baseType + "|" + ninjaEntry.itemClass;

                                switch (ninjaEntry.name) {
                                    case "Vessel of Vinktar":
                                        switch (ninjaEntry.variant) {
                                            case "Added Attacks":
                                                itemKey += "|var:attacks";
                                                break;
                                            case "Added Spells":
                                                itemKey += "|var:spells";
                                                break;
                                            case "Penetration":
                                                itemKey += "|var:penetration";
                                                break;
                                            case "Conversion":
                                                itemKey += "|var:conversion";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    case "Doryani's Invitation":
                                        switch (ninjaEntry.variant) {
                                            case null: // Bug on poe.ninja's end
                                            case "Physical":
                                                itemKey += "|var:physical";
                                                break;
                                            case "Fire":
                                                itemKey += "|var:fire";
                                                break;
                                            case "Cold":
                                                itemKey += "|var:cold";
                                                break;
                                            case "Lightning":
                                                itemKey += "|var:lightning";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    case "Impresence":
                                        switch (ninjaEntry.variant) {
                                            case "Chaos":
                                                itemKey += "|var:chaos";
                                                break;
                                            case "Physical":
                                                itemKey += "|var:physical";
                                                break;
                                            case "Fire":
                                                itemKey += "|var:fire";
                                                break;
                                            case "Cold":
                                                itemKey += "|var:cold";
                                                break;
                                            case "Lightning":
                                                itemKey += "|var:lightning";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    default:
                                        break;
                                }

                                if (!prices.ContainsKey(itemKey)) prices.Add(itemKey, entry);
                                break;

                            case "Essence":
                            case "DivinationCards":
                            case "Prophecy":
                                entry.value = ninjaEntry.chaosValue;

                                itemKey = ninjaEntry.name + "|" + ninjaEntry.itemClass;
                                if (!prices.ContainsKey(itemKey)) prices.Add(itemKey, entry);
                                break;

                            case "Map":
                                entry.value = ninjaEntry.chaosValue;

                                itemKey = ninjaEntry.name + "|0";
                                if (!prices.ContainsKey(itemKey)) prices.Add(itemKey, entry);
                                break;

                            case "SkillGem":
                                entry.value = ninjaEntry.chaosValue;

                                itemKey = ninjaEntry.name + "|" + ninjaEntry.itemClass + "|" + ninjaEntry.gemLevel + "|" + ninjaEntry.gemQuality;
                                if (ninjaEntry.corrupted) itemKey += "|1";
                                else itemKey += "|0";

                                if (!prices.ContainsKey(itemKey)) prices.Add(itemKey, entry);
                                break;
                        }
                    }
                } catch (Exception ex) {
                    MainWindow.Log(ex.ToString(), 2);
                }
            }
        }

        /// <summary>
        /// Uses machiene learning website http://poeprices.info to price magic/rare items
        /// </summary>
        /// <param name="rawItemData">Ctrl+C'd raw item data</param>
        /// <returns>Suggested price as double, 0 if unsuccessful</returns>
        public Entry SearchPoePrices(string rawItemData) {
            Entry returnEntry = new Entry() { count = 20 };

            try {
                // Make request to http://poeprices.info
                string jsonString = MainWindow.webClient.DownloadString("https://www.poeprices.info/api?l=" + Settings.league + 
                    "&i=" + MiscMethods.Base64Encode(rawItemData));

                // Deserialize JSON-encoded reply string
                PoePricesReply reply = new JavaScriptSerializer().Deserialize<PoePricesReply>(jsonString);

                // Some protection
                if (reply == null || reply.error != "0") return null;

                // If the price was in exalts, convert it to chaos
                if (reply.currency == "exalt") {
                    Entry exaltedEntry;
                    prices.TryGetValue("Exalted Orb|5", out exaltedEntry);
                    if (exaltedEntry == null) return null;
                    returnEntry.value = (reply.max * exaltedEntry.value + reply.min * exaltedEntry.value) / 2.0;
                } else {
                    returnEntry.value = (reply.max + reply.min) / 2.0;
                }

                // Return the constructed entry
                return returnEntry;
            } catch (Exception ex) {
                MainWindow.Log(ex.ToString(), 2);
                return null;
            }
        }

        /// <summary>
        /// Primitive method for looking up gem prices
        /// </summary>
        /// <param name="key">Database key to search for</param>
        /// <returns>Median value in chaos</returns>
        public Entry Search(string key) {
            Console.WriteLine(key);
            // Get the database entry
            Entry tempEntry;
            prices.TryGetValue(key, out tempEntry);

            // Precaution
            if (tempEntry == null) return null;

            // Make a copy so the original database entry is not affected
            return new Entry(tempEntry);
        }

        /// <summary>
        /// Formats the buyout note that will be pasted on the item
        /// </summary>
        /// <param name="price">Price that will be present in the note</param>
        /// <returns>Formatted buyout note (e.g. "~b/o 53.2 chaos")</returns>
        public string MakeNote(double price) {
            // Replace "," with "." due to game limitations
            return Settings.prefix + " " + price.ToString().Replace(',', '.') + " chaos";
        }
    }
}
