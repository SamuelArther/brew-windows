class ChooseGui < Formula
  desc "Fuzzy matcher that uses std{in,out} and a native GUI"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "choose", "choose"
  end

  test do
    system bin/"choose", "-h"
    output = pipe_output("#{bin}/choose -o ba", "foo\nbar\nbaz\n", 0)
    assert_match "bar", output
    refute_match "foo", output
  end
end
