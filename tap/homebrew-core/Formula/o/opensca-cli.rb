class OpenscaCli < Formula
  desc "OpenSCA is a supply-chain security tool for security researchers and developers"
  homepage "https://opensca.xmirror.cn"
  url "https://github.com/XmirrorSecurity/OpenSCA-cli/releases/download/v3.0.11/opensca-cli-v3.0.11-windows-amd64.zip"
  sha256 "534f3e50d246ffb77b2ef9f43bf1c15fd7f882c39e90374d942aa589fe739893"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/XmirrorSecurity/OpenSCA-cli/archive/refs/tags/v3.0.11.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/opensca-cli.exe", "opensca-cli"
  end

  test do
    system bin/"opensca-cli", "-path", testpath
    assert_path_exists testpath/"opensca.log"
    assert_match version.to_s, shell_output("#{bin}/opensca-cli -version")
  end
end
