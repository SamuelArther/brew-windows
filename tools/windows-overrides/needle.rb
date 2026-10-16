class Needle < Formula
  desc "Compile-time safe Swift dependency injection framework with real code"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "needle", "needle"
  end

  test do
    (testpath/"Test.swift").write <<~SWIFT
      import Foundation

      protocol ChildDependency: Dependency {}
      class Child: Component<ChildDependency> {}

      let child = Child(parent: self)
    SWIFT
    assert_match "Root\n", shell_output("#{bin}/needle print-dependency-tree #{testpath}/Test.swift")
    assert_match "0.25.1", shell_output("#{bin}/needle version")
    (testpath/"App.swift").write <<~SWIFT
      class RootComponent: BootstrapComponent {
        var api: API { shared { API() } }
        var child: ChildComponent { ChildComponent(parent: self) }
      }
      protocol ChildDependency: Dependency {
        var api: API { get }
      }
      class ChildComponent: Component<ChildDependency> {}
    SWIFT
    system bin/"needle", "generate", testpath/"Generated.swift", testpath/"App.swift"
    generated = (testpath/"Generated.swift").read
    assert_match "return rootComponent.api", generated
    assert_match "^->RootComponent->ChildComponent", generated
  end
end
