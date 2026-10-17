class KubectlExplore < Formula
  desc "Better kubectl explain with the fuzzy finder"
  homepage "https://github.com/keisku/kubectl-explore"
  url "https://github.com/keisku/kubectl-explore/archive/refs/tags/v0.14.1.tar.gz"
  sha256 "cf13b9a569d5fb3852e6d6d6844e3f4383e4dbddfd16187eccd05627a4c10e90"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "cec559241a53655e92c4109ff72478ea58fd39bdebb3bcaedd3493cec62cd4ef"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    # Windows words a refused connection "No connection could be made because the target machine actively refused it".
    assert_match OS.windows? ? "actively refused it" : "The connection to the server localhost:8080 was refused",
      shell_output("#{bin}/kubectl-explore pod 2>&1", 1)
  end
end
