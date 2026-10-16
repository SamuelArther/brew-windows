class TerraformProviderLibvirt < Formula
  desc "Terraform provisioning with Linux KVM using libvirt"
  homepage "https://github.com/dmacvicar/terraform-provider-libvirt"
  url "https://github.com/dmacvicar/terraform-provider-libvirt/releases/download/v0.9.9/terraform-provider-libvirt_0.9.9_windows_amd64.zip"
  sha256 "38216229290a3450d53a8a2799175fd8de5c26581913e20c8848b75890343ea3"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dmacvicar/terraform-provider-libvirt/archive/refs/tags/v0.9.9.tar.gz"
  end

  def install
    bin.install "terraform-provider-libvirt_v0.9.9.exe" => "terraform-provider-libvirt.exe"
  end

  test do
    system bin/"terraform-provider-libvirt", "--help"
  end
end
