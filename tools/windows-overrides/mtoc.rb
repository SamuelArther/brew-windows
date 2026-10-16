class Mtoc < Formula
  desc "Mach-O to PE/COFF binary converter"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "mtoc", "mtoc"
  end

  test do
    # A minimal x86_64 MH_PRELOAD image like clang -nostdlib -Wl,-preload builds: __TEXT at 0x1000,
    # one __text section at 0x1200 (xor eax,eax; ret) and an LC_UNIXTHREAD entry point there.
    macho = [0xfeedfacf, 0x01000007, 3, 5, 2, 336, 0, 0,
             0x19, 152, "__TEXT", 0x1000, 0x1000, 0, 0x204, 5, 5, 1, 0,
             "__text", "__TEXT", 0x1200, 4, 0x200, 4, 0, 0, 0x80000400, 0, 0, 0,
             5, 184, 4, 42, 0x1200, 0x90c3c031]
    (testpath/"test").binwrite macho.pack("V8V2a16Q<4V4a16a16Q<2V8V4x128Q<x32x144V")
    system bin/"mtoc", "-subsystem", "UEFI_APPLICATION", testpath/"test", testpath/"test.pe"
    pe = (testpath/"test.pe").binread
    assert_equal "MZ", pe[0, 2]
    assert_equal "PE\0\0", pe[0x40, 4]
    assert_match "MH_PRELOAD -> PE32+", shell_output("#{bin}/mtoc -v #{testpath}/test #{testpath}/test2.pe")
    assert_match "Usage: mtoc", shell_output("#{bin}/mtoc -h")
  end
end
