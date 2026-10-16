class Libgudev < Formula
  desc "Device queries and uevent monitoring on the Windows device tree (gudev-query)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
