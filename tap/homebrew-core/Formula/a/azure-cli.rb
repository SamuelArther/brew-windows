class AzureCli < Formula
  desc "Microsoft Azure CLI 2.0"
  homepage "https://docs.microsoft.com/cli/azure/overview"
  url "https://github.com/Azure/azure-cli/releases/download/azure-cli-2.91.0/azure-cli-2.91.0-x64.zip"
  sha256 "c0c49639a96d8c0dfb9973cfe08c97c5eeb69be050102138ede806285e6fcf1f"
  license "MIT"

  # Not a homebrew-core formula any more: Homebrew ships Azure CLI as the azure-cli cask.
  # desc/homepage come from that cask; license is upstream's MIT. Uses Microsoft's official
  # Windows x64 zip (bundled Python), which is what the MSI installs.

  def install
    install_tree
    windows_cmd "libexec/bin/az.cmd", "az"
  end

  test do
    version_output = shell_output("#{bin}/az --version")
    assert_match "azure-cli", version_output
    assert_match version.to_s, version_output
  end
end
