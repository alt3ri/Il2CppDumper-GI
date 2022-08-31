using System;
using System.IO;

namespace Il2CppDumper {
    public static class KhangSearch {
        private static ulong ToULong(byte[] a, int start) {
            return (ulong)(a[start + 0] | (a[start + 1] << 8) | (a[start + 2] << 16) | (a[start + 3] << 24));
        }

        public static bool SearchRegistrations(ulong imagebase, string filename, out ulong codeRegistration, out ulong metadataRegistration, out ulong mihoyoUsages) {
            codeRegistration = 0;
            metadataRegistration = 0;
            mihoyoUsages = 0;
            // custom search
            // searching .text for the following pattern:
            // lea r9,  [rip+0x????????]
            // lea r8,  [rip+0x????????]
            // lea rdx, [rip+0x????????]
            // lea rcx, [rip+0x????????]
            // jmp [rip+0x????????]
            // or...
            // 4c 8d 0d ?? ?? ?? ??
            // 4c 8d 05 ?? ?? ?? ??
            // 48 8d 15 ?? ?? ?? ??
            // 48 8d 0d ?? ?? ?? ??
            // e9
            // 29 bytes long

            var bytes = File.ReadAllBytes(filename);

            // functions are always aligned to 16 bytes
            const int patternLength = 29;
            for (int i = 0; i < bytes.Length - patternLength; i += 0x10) {
                if (
                    bytes[i + 0] == 0x4C && bytes[i + 1] == 0x8D && bytes[i + 2] == 0x0D &&
                    bytes[i + 7] == 0x4C && bytes[i + 8] == 0x8D && bytes[i + 9] == 0x05 &&
                    bytes[i + 14] == 0x48 && bytes[i + 15] == 0x8D && bytes[i + 16] == 0x15 &&
                    bytes[i + 21] == 0x48 && bytes[i + 22] == 0x8D && bytes[i + 23] == 0x0D &&
                    bytes[i + 28] == 0xE9
                ) {
                    codeRegistration = (ulong)i + 28 + ToULong(bytes, i + 21 + 3);
                    metadataRegistration = (ulong)i + 21 + ToULong(bytes, i + 14 + 3);
                    mihoyoUsages = (ulong)i + 14 + ToULong(bytes, i + 7 + 3);

                    Console.WriteLine($"Found the offsets! codeRegistration: 0x{codeRegistration:X}, metadataRegistration: 0x{metadataRegistration:X}, mihoyoUsages: 0x{mihoyoUsages:X}");
                    break;
                }
            }

            if (codeRegistration == 0 && metadataRegistration == 0 && mihoyoUsages == 0) {
                Console.WriteLine("Failed to find CodeRegistration, MetadataRegistration and MihoyoUsages, go yell at Khang or nitro");
                return false;
            }

            ulong bas = imagebase + 3072;

            codeRegistration += bas;
            metadataRegistration += bas;
            mihoyoUsages += bas;

            return true;
        }
    }
}
