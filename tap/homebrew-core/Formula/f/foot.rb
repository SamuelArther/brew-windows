class Foot < Formula
  desc "Fast, lightweight terminal launcher using Windows Terminal and foot.ini"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "foot", "foot"
    windows_exe "libexec/wintools.exe", "footclient", "footclient"
  end

  test do
    config_dir = testpath/".config/foot"
    config_file = config_dir/"foot.ini"
    mkdir_p config_dir
    config_file.write <<~INI
      [cursor]
      style=blok
    INI
    assert_match(
      /blok: not one of 'block', 'underline', 'beam'/,
      shell_output("#{bin}/foot --check-config 2>&1", 230),
    )
    rm(config_file)
    config_file.write <<~INI
      [cursor]
      style=block
    INI
    assert_empty shell_output("#{bin}/foot --check-config")
    assert_match "1.28.0", shell_output("#{bin}/footclient --version")
  end
end
