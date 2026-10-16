class Azqr < Formula
  desc "Azure Quick Review"
  homepage "https://azure.github.io/azqr/"
  url "https://github.com/Azure/azqr/releases/download/v.4.2.0/azqr-win-amd64.zip"
  version "4.2.0"
  sha256 "68efc992445a424e1a89c2d312c1a30059a93dc627f7cf1b2848c1e392290e4e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Azure/azqr.git"
  end

  def install
    install_tree
    windows_exe "libexec/windows_amd64/azqr.exe", "azqr"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/azqr -v")
    output = shell_output("#{bin}/azqr scan --filters notexists.yaml 2>&1", 1)
    assert_includes output, "failed reading data from file"
    output = shell_output("#{bin}/azqr scan 2>&1", 1)
    assert_includes output, "Failed to list subscriptions"
  end
end
