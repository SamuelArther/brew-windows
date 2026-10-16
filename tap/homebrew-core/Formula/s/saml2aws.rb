class Saml2aws < Formula
  desc "Login and retrieve AWS temporary credentials using a SAML IDP"
  homepage "https://github.com/Versent/saml2aws"
  url "https://github.com/Versent/saml2aws/releases/download/v2.36.19/saml2aws_2.36.19_windows_amd64.zip"
  sha256 "739bd99db011825521cf7adb15c205e7aac6570f8b74cd0afc5bedbb0804a0eb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Versent/saml2aws/archive/refs/tags/v2.36.19.tar.gz"
  end

  def install
    bin.install "saml2aws.exe"
  end

  test do
    assert_match "error building login details: Failed to validate account.: URL empty in idp account",
      shell_output("#{bin}/saml2aws script 2>&1", 1)

    assert_match version.to_s, shell_output("#{bin}/saml2aws --version 2>&1")
  end
end
