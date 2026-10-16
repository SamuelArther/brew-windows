class Bartycrouch < Formula
  desc "Incrementally update/translate your Strings files"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "bartycrouch", "bartycrouch"
  end

  test do
    (testpath/"Test.swift").write <<~SWIFT
      import Foundation

      class Test {
        func test() {
            NSLocalizedString("test", comment: "")
        }
      }
    SWIFT
    (testpath/"en.lproj").mkpath
    (testpath/"en.lproj/Localizable.strings").write <<~EOS
      /* No comment provided by engineer. */
      "oldKey" = "Some translation";
    EOS
    system bin/"bartycrouch", "update"
    assert_match '"oldKey" = "', (testpath/"en.lproj/Localizable.strings").read
    assert_match '"test" = "', (testpath/"en.lproj/Localizable.strings").read
    assert_match "Empty Values", shell_output("#{bin}/bartycrouch lint --path #{testpath}")
  end
end
