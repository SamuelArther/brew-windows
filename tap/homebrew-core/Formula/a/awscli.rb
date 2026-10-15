class Awscli < Formula
  desc "Official Amazon AWS command-line interface"
  homepage "https://aws.amazon.com/cli/"
  version "2.38.1"
  url "https://awscli.amazonaws.com/AWSCLIV2-2.38.1.msi"
  sha256 "ad07f9acd855f48ad016e63b2f0b4f87783c1b6524b86e966f4a6d21d01f524a"
  license "Apache-2.0"

  # Official AWS CLI v2 MSI read as data only. No installer actions or machine-wide installation.

  def install
    install_tree
    windows_exe "libexec/**/aws.exe", "aws"
  end
end
