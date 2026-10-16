class Prqlc < Formula
  desc "Simple, powerful, pipelined SQL replacement"
  homepage "https://prql-lang.org"
  url "https://github.com/PRQL/prql/releases/download/0.13.14/prqlc-0.13.14-x86_64-pc-windows-msvc.zip"
  sha256 "fa8cb13b3871842c65de5f4101e7371b03191f0ddcb7a9d0c0e5feceff9b3f3c"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/PRQL/prql/archive/refs/tags/0.13.14.tar.gz"
  end

  def install
    bin.install "prqlc.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/prqlc --version")

    stdin = "from employees | filter has_dog | select salary"
    stdout = pipe_output("#{bin}/prqlc compile", stdin)
    assert_match "SELECT", stdout
  end
end
