class Blades < Formula
  desc "Blazing fast dead simple static site generator"
  homepage "https://www.getblades.org/"
  url "https://github.com/grego/blades/archive/refs/tags/v0.6.0.tar.gz"
  sha256 "6bcce947580243e83a9bf4d6ec4afbc7e6cd0c7541a16d904c7d4f1314036bd0"
  license "GPL-3.0-or-later"
  head "https://github.com/grego/blades.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "717f2e5d9d6e007ad40eddf52205d1fb108631dbb2d9438567ff9760f728ef04"
  end

  depends_on "rust" => :build

  patch do
    url "https://github.com/grego/blades/commit/dccfd8e30b41309929184386040c8da918f08162.patch?full_index=1"
    sha256 "07507ad167f1cabfa1b479c2658c5e3212611ecb876a9c9daeccbdda29d087b2"
    type :unofficial
    resolves "https://github.com/grego/blades/pull/29"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/blades version")

    require "expect"
    require "pty"

    timeout = 5
    PTY.spawn(bin/"blades", "init") do |r, w, pid|
      refute_nil r.expect("Name:", timeout), "Expected name input"
      w.write "brew\r"
      refute_nil r.expect("Author:", timeout), "Expected author input"
      w.write "test\r"
      w.write "Y\r" # `Start with a minimal working template?`
      Process.wait pid
    end

    assert_path_exists testpath/"content"
    assert_match "title = \"brew\"", (testpath/"Blades.toml").read
  end
end
