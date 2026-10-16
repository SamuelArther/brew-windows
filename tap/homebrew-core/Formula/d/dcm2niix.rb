class Dcm2niix < Formula
  desc "DICOM to NIfTI converter"
  homepage "https://www.nitrc.org/plugins/mwiki/index.php/dcm2nii:MainPage"
  url "https://github.com/rordenlab/dcm2niix/releases/download/v1.0.20260724/dcm2niix_win.zip"
  sha256 "8310549ef7340b1b169c26f4721aeac69423fd73643fbbd22eec47b36cc70ada"
  license "BSD-3-Clause"
  version_scheme 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rordenlab/dcm2niix/archive/refs/tags/v1.0.20260724.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/dcm2niix.exe", "dcm2niix"
  end

  test do
    system bin/"dcm2niix", "-h"
  end
end
