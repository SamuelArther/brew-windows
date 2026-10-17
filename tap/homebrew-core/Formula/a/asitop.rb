class Asitop < Formula
  desc "Perf monitoring CLI tool for CPU tiers, GPU, power and memory"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "74e61983805debf416644a8b34ae046be13220a881ef897c5d7d0c04af3aeab3"
  end

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "asitop", "asitop"
  end

  test do
    assert_match "Performance monitoring CLI tool", shell_output("#{bin}/asitop --help")
    assert_match "RAM Usage", shell_output("#{bin}/asitop --max_count 1")
  end
end
