class Diskonaut < Formula
  desc "Terminal visual disk space navigator"
  homepage "https://github.com/imsnif/diskonaut"
  url "https://github.com/imsnif/diskonaut/archive/refs/tags/0.11.0.tar.gz"
  sha256 "355367dbc6119743d88bfffaa57ad4f308596165a57acc2694da1277c3025928"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8b8948e984b4255f98413a74069e256d1e76cbc788036a7603550258e22487ad"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    # On Windows the TUI reads the console, not the piped stdin, so it waits instead of failing.
    unless OS.windows?
      output = pipe_output(bin/"diskonaut", shell_output("ls"), 2)
      assert_match "Error: IO-error occurred", output
    end

    assert_match "diskonaut #{version}", shell_output("#{bin}/diskonaut --version")
  end
end
