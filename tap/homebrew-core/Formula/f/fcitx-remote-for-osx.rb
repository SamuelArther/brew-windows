class FcitxRemoteForOsx < Formula
  desc "Handle input method in command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "fcitx-remote", "fcitx-remote"
  end

  test do
    system bin/"fcitx-remote", "-n"
    assert_match(/^\h{8}$/, shell_output("#{bin}/fcitx-remote -n"))
    assert_match(/^[12]$/, shell_output("#{bin}/fcitx-remote"))
    assert_match "no installed input source", shell_output("#{bin}/fcitx-remote -s xx-NOPE 2>&1", 1)
  end
end
