class WaylandProtocols < Formula
  desc "Additional Wayland protocols"
  homepage "https://wayland.freedesktop.org"
  url "https://gitlab.freedesktop.org/wayland/wayland-protocols/-/releases/1.49/downloads/wayland-protocols-1.49.tar.xz"
  sha256 "ec4c8f74942d6dff7ace8b4ce4764f0ef9ff618a935d974ea77edee2ad240b14"
  license "MIT"

  # wayland-protocols is data: protocol XML files and a pkg-config file pointing at them. Meson's
  # install only copies them, so the keg is laid out the same way here without a build:
  # share/wayland-protocols/<stability>/<protocol>/<protocol>.xml and share/pkgconfig/wayland-protocols.pc.
  # wayland-scanner (from the wayland formula) turns them into C code on Windows as anywhere else.

  livecheck do
    url "https://wayland.freedesktop.org/releases.html"
    regex(/href=.*?wayland-protocols[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    %w[stable staging unstable experimental].each do |stability|
      next unless (buildpath/stability).directory?

      Dir["#{stability}/**/*.xml"].each do |xml|
        (share/"wayland-protocols"/File.dirname(xml)).install xml
      end
    end
    (share/"pkgconfig/wayland-protocols.pc").write <<~PC
      prefix=${pcfiledir}/../..
      datarootdir=${prefix}/share
      pkgdatadir=${pc_sysrootdir}${datarootdir}/wayland-protocols

      Name: Wayland Protocols
      Description: Wayland protocol files
      Version: #{version}
    PC
    doc.install "README.md", "COPYING"
  end

  test do
    assert_match "xdg_wm_base", (share/"wayland-protocols/stable/xdg-shell/xdg-shell.xml").read
    assert_path_exists share/"wayland-protocols/stable/viewporter/viewporter.xml"
    assert_match "Version: #{version}", (share/"pkgconfig/wayland-protocols.pc").read
  end
end
