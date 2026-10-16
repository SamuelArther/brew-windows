class WoofDoom < Formula
  desc "Woof! is a continuation of the Boom/MBF bloodline of Doom source ports"
  homepage "https://fabiangreffrath.github.io/woof/"
  url "https://github.com/fabiangreffrath/woof/releases/download/woof_16.0.0/Woof-16.0.0-win64.zip"
  sha256 "1c7fd0afe9b1ab9517ab900fc4b4974282430e50cb57fe3291d6218f125ccd31"
  license all_of: [
    # Default license is GPL-2.0-or-later but `woof` binary ends up GPL-3.0-or-later
    "GPL-2.0-or-later",
    "GPL-3.0-or-later", # src/v_flextran.*, src/v_video.*

    # Other licenses
    "BSD-2-Clause", # third-party/spng/*
    "BSD-3-Clause", # src/m_scanner.*, base/all-all/sprites/pls*, man/simplecpp
    "CC-BY-3.0",    # base/all-all/sm*.png, data/setup.ico, data/woof*, setup/setup_icon.c, src/icon.c
    "CC0-1.0",      # base/all-all/sbardef.lmp, data/io.github.fabiangreffrath.woof.metainfo.*
    "GPL-2.0-only", # soundfonts/TimGM6mb.sf2
    "MIT",          # src/i_flickstick.*, src/i_gyro.*, src/nano_bsp.*, base/all-all/dmxopl.op2, third-party/miniz/*
    "NCL",          # third-party/pffft/*
    :public_domain, # third-party/md5/*
    "CC-BY-SA-4.0", # textscreen/fonts/hauge-8x18-v1-6.png
    "Zlib",         # netlib
  ]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/fabiangreffrath/woof/archive/refs/tags/woof_16.0.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/woof.exe", "woof"
    windows_exe "libexec/woof-setup.exe", "woof-setup"
  end

  test do
    assert_path_exists bin/"woof.exe"
  end
end
