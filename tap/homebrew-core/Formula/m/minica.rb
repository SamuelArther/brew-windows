class Minica < Formula
  desc "Small, simple certificate authority"
  homepage "https://github.com/jsha/minica"
  url "https://github.com/jsha/minica/archive/refs/tags/v1.1.0.tar.gz"
  sha256 "4f56ea73d2a943656f8a5b533e554b435bc10f56c12d0b53836e84a96b513bf7"
  license "MIT"
  head "https://github.com/jsha/minica.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b51ae7e2a54438387d69b40b41899683c9240836d5e656120d1f8a6128b2db45"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    system "go", "build", *std_go_args
  end

  test do
    system bin/"minica", "--domains", "foo.com"
    assert_path_exists testpath/"minica.pem"
  end
end
