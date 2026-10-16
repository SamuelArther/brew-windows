class CargoInstruments < Formula
  desc "Easily generate Windows Performance Recorder traces for your rust crate"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "cargo-instruments", "cargo-instruments"
  end

  test do
    output = shell_output("#{bin}/cargo-instruments instruments --template sys 2>&1", 1)
    assert_match "could not find `Cargo.toml`", output
    assert_match "Time Profiler", shell_output("#{bin}/cargo-instruments instruments --list-templates")
    assert_match "--template", shell_output("#{bin}/cargo-instruments instruments --help")
    assert_match "unknown template", shell_output("#{bin}/cargo-instruments instruments -t nope 2>&1", 2)
  end
end
