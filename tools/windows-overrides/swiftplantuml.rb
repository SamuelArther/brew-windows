class Swiftplantuml < Formula
  desc "Generate UML class diagrams from Swift sources"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
