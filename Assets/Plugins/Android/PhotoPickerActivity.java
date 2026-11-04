package com.brenpoly.cardboardxr;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.ImageDecoder;
import android.net.Uri;
import android.os.Bundle;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.io.File;
import java.io.FileOutputStream;

// Lets the app pick a picture from the gallery: opens the system picker, shrinks what was picked so its
// longest side is at most maxSide, saves it in the app's cache and tells a Unity object (UnitySendMessage)
// its path, or "" if nothing was picked. Started from Unity with pick().
public class PhotoPickerActivity extends Activity {
    private static final String TAG = "CardboardXR";
    private static final int REQUEST = 4711;

    private static String sReceiver = "";
    private static String sMethod = "";
    private static int sMaxSide = 2048;

    public static void pick(Activity from, String receiver, String method, int maxSide) {
        sReceiver = receiver;
        sMethod = method;
        sMaxSide = maxSide;
        from.startActivity(new Intent(from, PhotoPickerActivity.class));
    }

    @Override
    protected void onCreate(Bundle state) {
        super.onCreate(state);
        if (state != null) {
            // Recreated while the picker was open: its result still arrives.
            return;
        }
        Intent intent = new Intent(Intent.ACTION_GET_CONTENT);
        intent.setType("image/*");
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        startActivityForResult(intent, REQUEST);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        final Uri uri = (resultCode == RESULT_OK && data != null) ? data.getData() : null;
        if (uri == null) {
            UnityPlayer.UnitySendMessage(sReceiver, sMethod, "");
            finish();
            return;
        }
        final Context context = getApplicationContext();
        final String receiver = sReceiver;
        final String method = sMethod;
        final int maxSide = sMaxSide;
        new Thread(new Runnable() {
            @Override
            public void run() {
                String path = decode(context, uri, maxSide);
                UnityPlayer.UnitySendMessage(receiver, method, path == null ? "" : path);
            }
        }).start();
        finish();
    }

    private static String decode(Context context, Uri uri, final int maxSide) {
        try {
            ImageDecoder.Source source = ImageDecoder.createSource(context.getContentResolver(), uri);
            Bitmap bitmap = ImageDecoder.decodeBitmap(source, new ImageDecoder.OnHeaderDecodedListener() {
                @Override
                public void onHeaderDecoded(ImageDecoder decoder, ImageDecoder.ImageInfo info, ImageDecoder.Source src) {
                    int longest = Math.max(info.getSize().getWidth(), info.getSize().getHeight());
                    int sample = 1;
                    while (longest / sample > maxSide) {
                        sample *= 2;
                    }
                    decoder.setTargetSampleSize(sample);
                    decoder.setAllocator(ImageDecoder.ALLOCATOR_SOFTWARE);
                }
            });
            deleteOld(context);
            boolean alpha = bitmap.hasAlpha();
            File out = new File(context.getCacheDir(), "photo_" + System.currentTimeMillis() + (alpha ? ".png" : ".jpg"));
            FileOutputStream stream = new FileOutputStream(out);
            try {
                bitmap.compress(alpha ? Bitmap.CompressFormat.PNG : Bitmap.CompressFormat.JPEG, 92, stream);
            } finally {
                stream.close();
            }
            bitmap.recycle();
            return out.getAbsolutePath();
        } catch (Throwable t) {
            Log.w(TAG, "Couldn't read the picked picture: " + t);
            return null;
        }
    }

    private static void deleteOld(Context context) {
        File[] files = context.getCacheDir().listFiles();
        if (files == null) {
            return;
        }
        for (File file : files) {
            if (file.getName().startsWith("photo_")) {
                file.delete();
            }
        }
    }
}
