class Betterglobekey < Formula
  desc "Globe-key style input language switching on a single key tap"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "betterglobekey", "betterglobekey"
  end

  def caveats
    <<~EOS
      betterglobekey runs in the background while you are signed in. To start it at login,
      put a shortcut to it in your Startup folder (Win+R, shell:startup).
      Configure the key and favourite input sources in %APPDATA%\betterglobekey\config.yaml
      (see betterglobekey --help).
    EOS
  end

  test do
    list = shell_output("#{bin}/betterglobekey list")
    assert_match(/^\h{8}\t[a-z]{2,3}(-\w+)*\t/, list)
    assert_match "betterglobekey", shell_output("#{bin}/betterglobekey version")
    assert_match "unknown key", shell_output("#{bin}/betterglobekey --key nokey 2>&1", 2)
  end
end
