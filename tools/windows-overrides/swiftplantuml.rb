class Swiftplantuml < Formula
  desc "Generate UML class diagrams from Swift sources"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "swiftplantuml", "swiftplantuml"
  end

  test do
    system bin/"swiftplantuml", "--help"
    (testpath/"Shapes.swift").write <<~SWIFT
      protocol Shape { func area() -> Double }
      class Base {}
      final class Circle: Base, Shape {
        var radius: Double = 1
        func area() -> Double { radius * radius * 3.14 }
      }
    SWIFT
    output = shell_output("#{bin}/swiftplantuml classdiagram Shapes.swift --output consoleOnly")
    assert_match "@startuml", output
    assert_match "class \"Circle\" as Circle", output
    assert_match "~radius : Double", output
    assert_match "Circle --|> Base : inherits", output
    assert_match "Circle ..|> Shape : conforms to", output
  end
end
