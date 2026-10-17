class Ktop < Formula
  desc "Top-like tool for your Kubernetes clusters"
  homepage "https://ktop.app/"
  url "https://github.com/vladimirvivien/ktop/archive/refs/tags/v0.5.3.tar.gz"
  sha256 "f255733a56ce292cdf6ca543cf5ef85969814de8ccf60431d70b85dfe38f720f"
  license "Apache-2.0"
  head "https://github.com/vladimirvivien/ktop.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c7add26bd0f3364c4502190fd9836e7fcfa732346d63985a2f5d7bc9a01cb39f"
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
