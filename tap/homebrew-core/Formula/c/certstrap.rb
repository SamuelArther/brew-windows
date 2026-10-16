class Certstrap < Formula
  desc "Tools to bootstrap CAs, certificate requests, and signed certificates"
  homepage "https://github.com/square/certstrap"
  url "https://github.com/square/certstrap/archive/refs/tags/v1.4.0.tar.gz"
  sha256 "10f1d123aa0ec066e3256741f1d945b5dfd2c61ca24855556e3a8db93b44d8c5"
  license "Apache-2.0"
  head "https://github.com/square/certstrap.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "eb8244b7d1a30159288c0ce1031eb58fbb19dfe84c8d202193d1af1971456140"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}")
  end

  test do
    system bin/"certstrap", "init", "--common-name", "Homebrew Test CA", "--passphrase", "beerformyhorses"
  end
end
