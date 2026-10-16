class Distrobox < Formula
  desc "Use any Linux distribution inside your terminal (on WSL)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "distrobox", "distrobox"
    windows_exe "libexec/wintools.exe", "distrobox-create", "distrobox-create"
    windows_exe "libexec/wintools.exe", "distrobox-enter", "distrobox-enter"
    windows_exe "libexec/wintools.exe", "distrobox-list", "distrobox-list"
    windows_exe "libexec/wintools.exe", "distrobox-rm", "distrobox-rm"
    windows_exe "libexec/wintools.exe", "distrobox-stop", "distrobox-stop"
    windows_exe "libexec/wintools.exe", "distrobox-upgrade", "distrobox-upgrade"
    windows_exe "libexec/wintools.exe", "distrobox-ephemeral", "distrobox-ephemeral"
    windows_exe "libexec/wintools.exe", "distrobox-export", "distrobox-export"
  end

  def caveats
    <<~EOS
      Distroboxes are WSL distributions, so WSL must be installed (wsl --install).
    EOS
  end

  test do
    assert_match "wsl.exe --install Ubuntu --name my-distrobox --no-launch", shell_output("#{bin}/distrobox-create --dry-run")
    assert_match "Ubuntu-22.04 --name test", shell_output("#{bin}/distrobox create --dry-run --image ubuntu:22.04 --name test")
    assert_match "FedoraLinux-43", shell_output("#{bin}/distrobox-create -d -i registry.fedoraproject.org/fedora-toolbox:43")
    assert_match "distrobox: #{version}", shell_output("#{bin}/distrobox version")
    assert_match "NAME", shell_output("#{bin}/distrobox-list --no-color")
  end
end
