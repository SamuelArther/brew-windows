class Rospo < Formula
  desc "Simple, reliable, persistent ssh tunnels with embedded ssh server"
  homepage "https://github.com/ferama/rospo"
  url "https://github.com/ferama/rospo/archive/refs/tags/v0.15.3.tar.gz"
  sha256 "4c36291969c84159baff261841e6ccd9520e17f8bc9142bf815b9097322a8857"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b1abf01d71de3880d4fd2ed12f9eee1d2014019603d77694860e51ca4917cd3c"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/ferama/rospo/cmd.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"rospo", shell_parameter_format: :cobra)
  end

  test do
    system bin/"rospo", "-v"
    system bin/"rospo", "keygen", "-s"
    assert_path_exists testpath/"identity"
    assert_path_exists testpath/"identity.pub"
  end
end
