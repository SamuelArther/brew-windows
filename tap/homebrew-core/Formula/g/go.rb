class Go < Formula
  desc "Open source programming language to build simple/reliable/efficient software"
  homepage "https://go.dev/"
  url "https://go.dev/dl/go1.27.2.windows-amd64.zip"
  version "1.27.2"
  sha256 "1314008898bd40df77af4b014f777f08873dbdfbcd3d92308728ee03304fe04f"
  license "BSD-3-Clause"

  livecheck do
    url "https://go.dev/dl/"
    regex(/href=.*?go[._-]?v?(\d+(?:\.\d+)+)[._-]windows[._-]amd64\.zip/i)
  end

  # Official Go binary distribution; GOROOT is the installed libexec tree.

  def install
    install_tree
    windows_exe "libexec/bin/go.exe", "go"
    windows_exe "libexec/bin/gofmt.exe", "gofmt"
  end

  test do
    (testpath/"hello.go").write <<~GO
      package main

      import "fmt"

      func main() {
        fmt.Println("Hello World")
      }
    GO
    system bin/"go", "fmt", "hello.go"
    assert_equal "Hello World\n", shell_output("#{bin}/go run hello.go")
  end
end
