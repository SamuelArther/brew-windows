class CfTerraforming < Formula
  desc "CLI to facilitate terraforming your existing Cloudflare resources"
  homepage "https://github.com/cloudflare/cf-terraforming"
  url "https://github.com/cloudflare/cf-terraforming/releases/download/v0.29.0/cf-terraforming_0.29.0_windows_amd64.tar.gz"
  sha256 "b120c3464457326e79299fd0afccba2094582e80fb406685ea32fc11ebf29189"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cloudflare/cf-terraforming/archive/refs/tags/v0.29.0.tar.gz"
  end

  def install
    bin.install "cf-terraforming.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cf-terraforming version")
    output = shell_output("#{bin}/cf-terraforming generate --token dummy 2>&1", 1)
    assert_match "you must define a resource type to generate", output
  end
end
