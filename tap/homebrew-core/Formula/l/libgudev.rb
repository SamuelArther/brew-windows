class Libgudev < Formula
  desc "Device queries and uevent monitoring on the Windows device tree (gudev-query)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "gudev-query", "gudev-query"
  end

  def caveats
    <<~EOS
      gudev-query lists and monitors the Windows Plug and Play devices the way GUdevClient
      does, as udev devices: subsystem, sysfs-style path, driver, udev properties and sysfs-style
      attributes, filtered with g_udev_client_query_by_subsystem's SUBSYSTEM[/DEVTYPE] syntax.
      There is no libgudev C library: Windows programs enumerate devices with SetupAPI/CfgMgr32.
    EOS
  end

  test do
    devices = shell_output("#{bin}/gudev-query --brief")
    assert_match "/sys/devices/", devices
    assert_match "Subsystem: ", shell_output("#{bin}/gudev-query")
    assert_equal "", shell_output("#{bin}/gudev-query --subsystem no_such_subsystem")
    assert_match "libgudev 238", shell_output("#{bin}/gudev-query --version")
    shell_output("#{bin}/gudev-query --bogus 2>&1", 2)
  end
end
