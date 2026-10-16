class Gofumpt < Formula
  desc "Stricter gofmt"
  homepage "https://github.com/mvdan/gofumpt"
  url "https://github.com/mvdan/gofumpt/releases/download/v0.12.0/gofumpt_v0.12.0_windows_amd64.exe"
  sha256 "66bd679908b64864fc85417e7ea01148774b477364b9a02ab1c194333a55c997"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mvdan/gofumpt/archive/refs/tags/v0.12.0.tar.gz"
  end

  def install
    bin.install "gofumpt_v0.12.0_windows_amd64.exe" => "gofumpt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gofumpt -version").split.first

    (testpath/"test.go").write <<~GO
      package foo

      func foo() {
        println("bar")

      }
    GO

    (testpath/"expected.go").write <<~GO
      package foo

      func foo() {
      	println("bar")
      }
    GO

    assert_match shell_output("#{bin}/gofumpt test.go"), (testpath/"expected.go").read
  end
end
