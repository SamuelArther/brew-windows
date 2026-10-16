class PipewireGstreamer < Formula
  desc "GStreamer audio source/sink plugin for the Windows audio graph (WASAPI)"
  homepage "https://gstreamer.freedesktop.org/documentation/wasapi2/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gst-plugins-bad-1.28.8-1-any.pkg.tar.zst"
  version "1.28.8"
  sha256 "ab5cd5f302019fe42904c3d8bc937728482dbbdebaaedd55bdf7b5ea2f04247c"
  license "LGPL-2.1-or-later"

  # pipewire-gstreamer is GStreamer's bridge to the PipeWire graph (pipewiresrc, pipewiresink and a
  # device provider). On Windows the audio graph is WASAPI, and GStreamer's bridge to it is the
  # wasapi2 plugin (wasapi2src, wasapi2sink, Wasapi2DeviceProvider), plus the older wasapi one.
  # This keg carries just those plugins from MSYS2's gst-plugins-bad build, and the GStreamer
  # libraries they link (gst-plugins-base's audio library and the bad-plugins helper libraries).

  livecheck do
    url "https://gstreamer.freedesktop.org/src/gst-plugins-bad/"
    regex(/href=.*?gst-plugins-bad[._-]v?(\d+\.\d*[02468](?:\.\d+)*)\.t/i)
  end

  depends_on "gstreamer"

  resource "mingw-w64-ucrt-x86_64-gst-plugins-base" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gst-plugins-base-1.28.8-1-any.pkg.tar.zst"
    sha256 "2c6e937a3e551eea5a139c47896517bb6fa6a00dfefcf3a0968e3175c1ba4eaf"
  end

  resource "mingw-w64-ucrt-x86_64-gst-plugins-bad-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gst-plugins-bad-libs-1.28.8-1-any.pkg.tar.zst"
    sha256 "793832176089b4cd12f6cbaee2d6149325af72682f8f3ca72d0fc805b39770ec"
  end

  resource "mingw-w64-ucrt-x86_64-orc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-orc-0.4.44-1-any.pkg.tar.zst"
    sha256 "f333a8207fbec0178e34d6293e412015c7450e3e73dfa1627279cd922f5d622f"
  end

  def install
    install_tree
    %w[gst-plugins-base gst-plugins-bad-libs orc].each do |name|
      install_resource "mingw-w64-ucrt-x86_64-#{name}", "libexec"
    end
    (lib/"gstreamer-1.0").install Dir["#{libexec}/ucrt64/lib/gstreamer-1.0/libgstwasapi*.dll"]
    # The libraries the plugins import, found on PATH when GStreamer loads them.
    bin.install Dir["#{libexec}/ucrt64/bin/*.dll"]
    rm_r libexec
  end

  def caveats
    <<~EOS
      The elements are wasapi2src and wasapi2sink (and wasapisrc/wasapisink), Windows' counterparts
      of pipewiresrc and pipewiresink. For GStreamer to find the plugins:
        set GST_PLUGIN_PATH=#{opt_lib}/gstreamer-1.0
    EOS
  end

  test do
    ENV["GST_PLUGIN_PATH"] = (opt_lib/"gstreamer-1.0").to_s
    assert_path_exists lib/"gstreamer-1.0/libgstwasapi2.dll"
    assert_match "wasapi2sink", shell_output("#{formula_opt_bin("gstreamer")}/gst-inspect-1.0 wasapi2")
  end
end
