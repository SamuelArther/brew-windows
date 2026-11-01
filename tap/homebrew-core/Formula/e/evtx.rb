class Evtx < Formula
  desc "Windows XML Event Log parser"
  homepage "https://github.com/omerbenamram/evtx"
  url "https://github.com/omerbenamram/evtx/archive/refs/tags/v0.15.1.tar.gz"
  sha256 "0b62f517a580f6afe30e9c6ad543e0e9d2763e47fc5d724a508526aa49e8c840"
  license any_of: ["Apache-2.0", "MIT"]
  head "https://github.com/omerbenamram/evtx.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a1499b11a1bd6d79645d826552ed94a3fa66954ca5285c72115194d84418f265"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args

    pkgshare.install "samples"
  end

  test do
    cp pkgshare/"samples/issue_201.evtx", testpath
    assert_match "Remote-ManagementShell-Unknown",
      shell_output("#{bin}/evtx_dump #{pkgshare}/samples/issue_201.evtx")

    assert_match "EVTX Parser #{version}", shell_output("#{bin}/evtx_dump --version")
  end
end
