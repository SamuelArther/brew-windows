class Maturin < Formula
  desc "Build and publish Rust crates as Python packages"
  homepage "https://github.com/PyO3/maturin"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-maturin-1.15.0-1-any.pkg.tar.zst"
  sha256 "d5f2c3f70a6a3ef6060707a103e9e0022ea997f881490d7d5ddc781464e4dc75"
  license any_of: ["Apache-2.0", "MIT"]
  revision 2

  # Prebuilt UCRT64 package from MSYS2 (maturin.exe only).

  livecheck do
    url "https://github.com/PyO3/maturin/archive/refs/tags/v1.15.0.tar.gz"
  end

  # Upstream's Python package: the PEP 517 backend (`build-backend = "maturin"`), which runs maturin.exe.
  resource "python-package" do
    url "https://github.com/PyO3/maturin/archive/refs/tags/v1.15.0.tar.gz"
    sha256 "623111bddb2d7f6f4ba2e64038f91f8b673bd6f95dd6fcbaf334b7af7789b48d"
  end

  def install
    # install_tree, not msys2_install, which would copy the source resource into libexec as well.
    install_tree
    msys2_bin "ucrt64/bin/maturin.exe", "maturin"

    # Like upstream: the newest Python gets the package and the others link to it, so builds without
    # build isolation (cryptography's `pip install --no-build-isolation`) can import the backend.
    resource("python-package").stage do
      newest_site_packages = lib/"python3.15/site-packages"
      newest_site_packages.install "maturin"
      %w[3.10 3.11 3.12 3.13 3.14].each do |pyver|
        (lib/"python#{pyver}/site-packages/maturin").install_symlink (newest_site_packages/"maturin").children
      end
    end
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/maturin --version 2>&1")
    assert_path_exists lib/"python3.14/site-packages/maturin/__init__.py"
  end
end
