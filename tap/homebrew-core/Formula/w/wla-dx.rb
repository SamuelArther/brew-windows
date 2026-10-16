class WlaDx < Formula
  desc "Yet another crossassembler package"
  homepage "https://github.com/vhelin/wla-dx"
  url "https://github.com/vhelin/wla-dx/releases/download/v10.7/wla_dx_v10.7_Win64.zip"
  sha256 "82922a530ae989bd5f8057fa4cfef3d0022d7353b5aeb7858b9320eae9b56ebd"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vhelin/wla-dx/archive/refs/tags/v10.7.tar.gz"
    regex(/v?(\d+(?:\.\d+)+)(?:-fix)*/i)
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/wla-6502.exe", "wla-6502"
    windows_exe "libexec/wla-65816.exe", "wla-65816"
    windows_exe "libexec/wla-65c02.exe", "wla-65c02"
    windows_exe "libexec/wla-65ce02.exe", "wla-65ce02"
    windows_exe "libexec/wla-6800.exe", "wla-6800"
    windows_exe "libexec/wla-68000.exe", "wla-68000"
    windows_exe "libexec/wla-6801.exe", "wla-6801"
    windows_exe "libexec/wla-6809.exe", "wla-6809"
    windows_exe "libexec/wla-8008.exe", "wla-8008"
    windows_exe "libexec/wla-8080.exe", "wla-8080"
    windows_exe "libexec/wla-cx4.exe", "wla-cx4"
    windows_exe "libexec/wla-ez80.exe", "wla-ez80"
    windows_exe "libexec/wla-gb.exe", "wla-gb"
    windows_exe "libexec/wla-huc6280.exe", "wla-huc6280"
    windows_exe "libexec/wla-sh2.exe", "wla-sh2"
    windows_exe "libexec/wla-spc700.exe", "wla-spc700"
    windows_exe "libexec/wla-superfx.exe", "wla-superfx"
    windows_exe "libexec/wla-z80.exe", "wla-z80"
    windows_exe "libexec/wla-z80n.exe", "wla-z80n"
    windows_exe "libexec/wlab.exe", "wlab"
    windows_exe "libexec/wlalink.exe", "wlalink"
  end

  test do
    (testpath/"test-gb-asm.s").write <<~ASM
      .MEMORYMAP
       DEFAULTSLOT 1.01
       SLOT 0.001 $0000 $2000
       SLOT 1.2 STArT $2000 sIzE $6000
       .ENDME

       .ROMBANKMAP
       BANKSTOTAL 2
       BANKSIZE $2000
       BANKS 1
       BANKSIZE $6000
       BANKS 1
       .ENDRO

       .BANK 1 SLOT 1

       .ORGA $2000

       ld hl, sp+127
       ld hl, sp-128
       add sp, -128
       add sp, 127
       adc 200
       jr -128
       jr 127
       jr nc, 127
    ASM

    system bin/"wla-gb", "-o", testpath/"test-gb-asm.s"
  end
end
