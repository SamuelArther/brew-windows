class Dbus < Formula
  desc "Message bus system, providing inter-application communication"
  homepage "https://wiki.freedesktop.org/www/Software/dbus"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dbus-1.16.2-4-any.pkg.tar.zst"
  sha256 "0fbb10a269563db51c7227e9dd909dbf0fdd9ca7f5c99a827ab7e1abf501ac9a"
  license any_of: ["AFL-2.1", "GPL-2.0-or-later"]
  revision 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://dbus.freedesktop.org/releases/dbus/"
    regex(/href=.*?dbus[._-]v?(\d+\.\d*?[02468](?:\.\d+)*)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/dbus-daemon.exe", "dbus-daemon"
    msys2_bin "ucrt64/bin/dbus-launch.exe", "dbus-launch"
    msys2_bin "ucrt64/bin/dbus-monitor.exe", "dbus-monitor"
    msys2_bin "ucrt64/bin/dbus-run-session.exe", "dbus-run-session"
    msys2_bin "ucrt64/bin/dbus-send.exe", "dbus-send"
    msys2_bin "ucrt64/bin/dbus-test-tool.exe", "dbus-test-tool"
    msys2_bin "ucrt64/bin/dbus-update-activation-environment.exe", "dbus-update-activation-environment"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dbus-daemon --version")
  end
end
