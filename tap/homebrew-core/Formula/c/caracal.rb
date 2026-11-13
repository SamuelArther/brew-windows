class Caracal < Formula
  desc "Static analyzer for Starknet smart contracts"
  homepage "https://github.com/crytic/caracal"
  url "https://github.com/crytic/caracal/archive/refs/tags/v0.2.3.tar.gz"
  sha256 "70a505b46d19cc389fa11bc17bed106e15ede6b076fb1f8b350a4ccabb4e7052"
  license "AGPL-3.0-only"
  head "https://github.com/crytic/caracal.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7048dfa35e492d1a56078e9ea08f7620dae925ef74b2142af7938cf9a7c29c14"
  end

  depends_on "rust" => :build

  resource "corelib", :test do
    url "https://github.com/starkware-libs/cairo/archive/refs/tags/v2.5.0.tar.gz"
    sha256 "0c21b58bc7ae2e8a6d47acedc4d20f30a41957deb6e24f8adaf31183112f8a4d"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args

    # sample test contracts
    pkgshare.install "tests/detectors"
  end

  test do
    resource("corelib").stage(testpath)
    assert_match("controlled-library-call Impact: High Confidence: Medium",
                 shell_output("#{bin}/caracal detect #{pkgshare}/detectors/controlled_library_call.cairo " \
                              "--corelib corelib/src/"))
  end
end
