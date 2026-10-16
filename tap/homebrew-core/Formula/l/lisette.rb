class Lisette < Formula
  desc "Language inspired by Rust that compiles to Go"
  homepage "https://lisette.run"
  url "https://github.com/ivov/lisette/releases/download/lisette-v0.12.4/lisette-x86_64-pc-windows-msvc.zip"
  version "0.12.4"
  sha256 "7402b484f411603bdd14feea06799330535407b7c3f7b5e729293d5c5d8ecd05"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ivov/lisette/archive/refs/tags/lisette-v0.12.4.tar.gz"
    regex(/^lisette[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "lis.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lis version")

    (testpath/"hello.lis").write <<~LIS
      import "go:fmt"

      fn main() {
        fmt.Println("hello")
      }
    LIS
    system bin/"lis", "check", testpath/"hello.lis"
  end
end
