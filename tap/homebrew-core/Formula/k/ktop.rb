class Ktop < Formula
  desc "Top-like tool for your Kubernetes clusters"
  homepage "https://ktop.app/"
  url "https://github.com/vladimirvivien/ktop/archive/refs/tags/v0.6.2.tar.gz"
  sha256 "4562cc50590640460d7cf7b28d28805db0dace76a70b0329f3cb51b96ea7bfe5"
  license "Apache-2.0"
  head "https://github.com/vladimirvivien/ktop.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3464e08ab07bf7dd0e028a790066ce67812753485c097248f218c0fc8c4858fd"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/vladimirvivien/ktop/buildinfo.Version=#{version}
      -X github.com/vladimirvivien/ktop/buildinfo.GitSHA=#{tap.user}
    ]
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    output = shell_output("#{bin}/ktop --all-namespaces 2>&1", 1)
    # Windows words a refused connection "No connection could be made because the target machine actively refused it".
    assert_match OS.windows? ? "actively refused it" : "connection refused", output
  end
end
