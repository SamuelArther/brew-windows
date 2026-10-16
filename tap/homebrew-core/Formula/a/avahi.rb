class Avahi < Formula
  desc "Service discovery over mDNS/DNS-SD using the Windows mDNS stack"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "avahi-browse", "avahi-browse"
    windows_exe "libexec/wintools.exe", "avahi-resolve", "avahi-resolve"
    windows_exe "libexec/wintools.exe", "avahi-resolve-host-name", "avahi-resolve-host-name"
    windows_exe "libexec/wintools.exe", "avahi-resolve-address", "avahi-resolve-address"
    windows_exe "libexec/wintools.exe", "avahi-publish", "avahi-publish"
    windows_exe "libexec/wintools.exe", "avahi-publish-service", "avahi-publish-service"
    windows_exe "libexec/wintools.exe", "avahi-publish-address", "avahi-publish-address"
    windows_exe "libexec/wintools.exe", "avahi-daemon", "avahi-daemon"
  end

  def caveats
    <<~EOS
      Windows' DNS Client service is the mDNS responder, so these are avahi's command-line
      tools only: there is no avahi-daemon to run and no libavahi to build against.
    EOS
  end

  test do
    assert_match "avahi-browse", shell_output("#{bin}/avahi-browse --help")
    shell_output("#{bin}/avahi-browse -t -k _http._tcp")
    assert_match "127.0.0.1", shell_output("#{bin}/avahi-resolve -4 -n localhost")
    assert_match "no separate daemon", shell_output("#{bin}/avahi-daemon 2>&1", 1)
  end
end
