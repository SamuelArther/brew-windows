class Azion < Formula
  desc "CLI for the Azion service"
  homepage "https://github.com/aziontech/azion"
  url "https://github.com/aziontech/azion/archive/refs/tags/4.26.2.tar.gz"
  sha256 "ed3da22048d5add0433863ba5de6340e5ecf2bc5bffc6d9c70736ffccc1d0447"
  license "MIT"
  head "https://github.com/aziontech/azion.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "bb7975e85048ada85a07865a37f51312cfe4722b8700ed5b9dceb8e004eed4d1"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/aziontech/azion-cli/pkg/cmd/version.BinVersion=#{version}
      -X github.com/aziontech/azion-cli/pkg/constants.ApiURL=https://api.azionapi.net
      -X github.com/aziontech/azion-cli/pkg/constants.ApiV4URL=https://api.azion.com/v4
      -X github.com/aziontech/azion-cli/pkg/constants.ApiV6URL=https://api.azion.com/v6
      -X github.com/aziontech/azion-cli/pkg/constants.AuthURL=https://sso.azion.com/api
      -X github.com/aziontech/azion-cli/pkg/constants.StorageApiURL=https://api.azion.com/v4
      -X github.com/aziontech/azion-cli/pkg/constants.StorageApiV6URL=https://api.azion.com/v6
    ]
    system "go", "build", *std_go_args(ldflags:), "./cmd/azion"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/azion --version")
    assert_match "Failed to build your resource", shell_output("#{bin}/azion build --yes 2>&1", 1)
  end
end
