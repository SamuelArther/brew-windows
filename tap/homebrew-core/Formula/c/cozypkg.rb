class Cozypkg < Formula
  desc "CLI for managing Cozystack packages"
  homepage "https://cozystack.io"
  url "https://github.com/cozystack/cozystack/archive/refs/tags/v1.7.1.tar.gz"
  sha256 "c0cfe16962c2501fa6db3e6f4b925ecbd28263f55679246be46167345d1b82a1"
  license "Apache-2.0"
  head "https://github.com/cozystack/cozystack.git", branch: "main"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "95a70ff5694361d5b3682f3c74c58b10d966d0ad2580b939befdd152136e9b5c"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[-X github.com/cozystack/cozystack/cmd/cozypkg/cmd.Version=#{version}]
    system "go", "build", *std_go_args(ldflags:), "./cmd/cozypkg"
    generate_completions_from_executable(bin/"cozypkg", "completion")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cozypkg --version")

    ENV["KUBECONFIG"] = testpath/"nonexistent-kubeconfig"
    output = shell_output("#{bin}/cozypkg list 2>&1", 1)
    assert_match "failed to get kubeconfig", output
    assert_match "try setting KUBERNETES_MASTER environment variable", output
  end
end
