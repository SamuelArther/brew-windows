class Tengo < Formula
  desc "Fast script language for Go"
  homepage "https://tengolang.com"
  url "https://github.com/d5/tengo/releases/download/v3.0.0/tengo_3.0.0_windows_amd64.tar.gz"
  sha256 "a4dbea7a15bd9e73d59500ff65861c42af9708b3a0767f3ec2b708341b8d81ca"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/d5/tengo/archive/refs/tags/v3.0.0.tar.gz"
  end

  def install
    bin.install "tengo.exe"
  end

  test do
    (testpath/"main.tengo").write <<~TENGO
      fmt := import("fmt")

      each := func(seq, fn) {
          for x in seq { fn(x) }
      }

      sum := func(init, seq) {
          each(seq, func(x) { init += x })
          return init
      }

      fmt.println(sum(0, [1, 2, 3]))   // "6"
      fmt.println(sum("", [1, 2, 3]))  // "123"
    TENGO
    assert_equal "6\n123\n", shell_output("#{bin}/tengo #{testpath}/main.tengo")
  end
end
