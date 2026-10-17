class Clib < Formula
  desc "Package manager for C programming"
  homepage "https://github.com/clibs/clib"
  url "https://github.com/clibs/clib/archive/refs/tags/2.8.7.tar.gz"
  sha256 "83d5767e363c3ed4b4271000b9ce63b6e11b6c4740df910e0074f844fb34258e"
  license "MIT"
  head "https://github.com/clibs/clib.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/clib-2.8.7"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "955a49e1b89b57a2020f5f767a3e5de210ef1f002c6400b4caeb7e31488ea811"
  end

  uses_from_macos "curl"

  def install
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/clib --version")

    pipe_output("#{bin}/clib init", "brewtest\n0.0.1\n", 0)
    assert_match "brewtest", (testpath/"clib.json").read
  end
end
