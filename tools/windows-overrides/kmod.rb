class Kmod < Formula
  desc "Kernel module handling for Windows kernel drivers"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "kmod", "kmod"
    windows_exe "libexec/wintools.exe", "lsmod", "lsmod"
    windows_exe "libexec/wintools.exe", "modinfo", "modinfo"
    windows_exe "libexec/wintools.exe", "insmod", "insmod"
    windows_exe "libexec/wintools.exe", "rmmod", "rmmod"
    windows_exe "libexec/wintools.exe", "modprobe", "modprobe"
    windows_exe "libexec/wintools.exe", "depmod", "depmod"
  end

  def caveats
    <<~EOS
      Modules are Windows kernel drivers (.sys). insmod/modprobe load them as driver services
      and rmmod stops them; those need an elevated (Administrator) prompt.
    EOS
  end

  test do
    system bin/"kmod", "help"
    assert_match "Module", shell_output("#{bin}/kmod list")
    assert_match "Used by", shell_output("#{bin}/lsmod")
    assert_match(/filename:.*ntfs\.sys/i, shell_output("#{bin}/modinfo ntfs"))
    assert_match "tcpip.sys", shell_output("#{bin}/modprobe --show-depends tcpip")
    assert_match "drivers/tcpip.sys:", shell_output("#{bin}/depmod -n")
    shell_output("#{bin}/modinfo no_such_module 2>&1", 1)
  end
end
