class Mmdbinspect < Formula
  desc "Look up records for one or more IPs/networks in one or more .mmdb databases"
  homepage "https://github.com/maxmind/mmdbinspect"
  url "https://github.com/maxmind/mmdbinspect/releases/download/v2.0.0/mmdbinspect_2.0.0_windows_amd64.zip"
  sha256 "8aa01679e75ce7aa8e41d4276a33039cd78a841db77a77f932614b68d543d8eb"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/maxmind/mmdbinspect/archive/refs/tags/v2.0.0.tar.gz"
  end

  def install
    bin.install "mmdbinspect.exe"
  end

  test do
    system bin/"mmdbinspect", "--help"
  end
end
