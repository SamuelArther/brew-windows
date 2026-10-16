class TfplugingenOpenapi < Formula
  desc "OpenAPI to Terraform Provider Code Generation Specification"
  homepage "https://github.com/hashicorp/terraform-plugin-codegen-openapi"
  url "https://github.com/hashicorp/terraform-plugin-codegen-openapi/releases/download/v0.3.0/tfplugingen-openapi_0.3.0_windows_amd64.zip"
  sha256 "aa5c0153cb304d867f12120c4e066fba43d9311b83c87504899b03f5be62b888"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hashicorp/terraform-plugin-codegen-openapi/archive/refs/tags/v0.3.0.tar.gz"
  end

  def install
    bin.install "tfplugingen-openapi.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tfplugingen-openapi --version 2>&1")
    assert_match "OpenAPI specification file is required", shell_output("#{bin}/tfplugingen-openapi generate 2>&1", 1)
  end
end
